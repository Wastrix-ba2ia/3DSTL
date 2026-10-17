"""Publish a verified desktop ZIP after committing and pushing its source.

Uses the configured Git credential helper; never writes credentials to disk.
Assets are uploaded to a draft before the stable release becomes visible.
"""
from pathlib import Path
import hashlib
import json
import re
import subprocess
import urllib.error
import urllib.parse
import urllib.request

root = Path(__file__).resolve().parents[1]
version = (root / 'VERSION').read_text().strip()
if not re.fullmatch(r'\d+\.\d+\.\d+', version):
    raise RuntimeError('Invalid VERSION')
if subprocess.check_output(['git', 'status', '--porcelain'], cwd=root, text=True).strip():
    raise RuntimeError('Commit all source changes before publishing')
commit = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
ref_request = urllib.request.Request('https://api.github.com/repos/Wastrix-ba2ia/3DSTL/git/ref/heads/main', headers={'User-Agent': '3DSTL-release'})
with urllib.request.urlopen(ref_request, timeout=30) as response:
    remote = json.load(response)['object']['sha']
if remote != commit:
    raise RuntimeError('Push this exact source commit to origin/main first')
name = f'3DSTL-{version}-win-x64.zip'
package = root / 'artifacts' / name
sums = root / 'artifacts' / 'SHA256SUMS.txt'
with package.open('rb') as stream:
    digest = hashlib.file_digest(stream, 'sha256').hexdigest()
if sums.read_text().strip() != f'{digest}  {name}':
    raise RuntimeError('Package checksum file does not match')
credential = subprocess.run(['git', 'credential', 'fill'], input='protocol=https\nhost=github.com\n\n', capture_output=True, text=True, check=True)
values = dict(line.split('=', 1) for line in credential.stdout.splitlines() if '=' in line)
headers = {'Authorization': 'Bearer ' + values['password'], 'Accept': 'application/vnd.github+json', 'User-Agent': '3DSTL-release'}
api = 'https://api.github.com/repos/Wastrix-ba2ia/3DSTL'


def request(url, body=None, method=None, content_type='application/json'):
    data = body if isinstance(body, bytes) else None if body is None else json.dumps(body).encode()
    req = urllib.request.Request(url, data=data, method=method, headers={**headers, 'Content-Type': content_type})
    with urllib.request.urlopen(req, timeout=300) as response:
        return json.load(response)


try:
    release = request(api + '/releases/tags/v' + version)
except urllib.error.HTTPError as error:
    if error.code != 404:
        raise
    release = request(api + '/releases', {
        'tag_name': 'v' + version, 'target_commitish': commit,
        'name': '3DSTL v' + version + ' · 大模型预览 / Large model previews',
        'body': (root / 'RELEASE-NOTES.md').read_text(encoding='utf-8-sig'),
        'draft': True, 'prerelease': False,
    })
if not release['draft']:
    raise RuntimeError('This version is already published; bump VERSION instead of replacing it')
for file in (package, sums):
    with file.open('rb') as stream:
        expected = hashlib.file_digest(stream, 'sha256').hexdigest()
    existing = next((asset for asset in release['assets'] if asset['name'] == file.name), None)
    if existing:
        if existing['size'] != file.stat().st_size or existing.get('digest') != 'sha256:' + expected:
            raise RuntimeError('Existing draft asset differs: ' + file.name)
        continue
    url = release['upload_url'].split('{')[0] + '?name=' + urllib.parse.quote(file.name)
    asset = request(url, file.read_bytes(), content_type='application/zip' if file.suffix == '.zip' else 'text/plain')
    if asset['size'] != file.stat().st_size or asset.get('digest') != 'sha256:' + expected:
        raise RuntimeError('Uploaded asset digest mismatch: ' + file.name)
    print('Verified uploaded asset:', file.name, flush=True)
release = request(api + '/releases/' + str(release['id']), {'draft': False, 'make_latest': 'true'}, method='PATCH')
print('Published:', release['html_url'], flush=True)
