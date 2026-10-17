param([string]$Python='python')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$out=Join-Path $root 'artifacts\3DSTL-0.1.0-win-x64'
$runtime=Join-Path $out 'cad-runtime'
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
$archive=Join-Path $root 'artifacts\python-embed.zip'
Invoke-WebRequest https://www.python.org/ftp/python/3.12.10/python-3.12.10-embed-amd64.zip -OutFile $archive
Expand-Archive -LiteralPath $archive -DestinationPath $runtime -Force
& $Python -m pip install --target (Join-Path $runtime 'Lib\site-packages') -r (Join-Path $root 'requirements-runtime.txt')
if($LASTEXITCODE -ne 0){throw 'Dependency install failed'}
[IO.File]::WriteAllText((Join-Path $runtime 'python312._pth'),"python312.zip`n.`n..`nLib/site-packages`nimport site`n")
$pre=Join-Path $out 'prerequisites';New-Item -ItemType Directory -Path $pre -Force | Out-Null
$installer=Join-Path $pre 'windowsdesktop-runtime-8.0.29-win-x64.exe'
Invoke-WebRequest https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/8.0.29/windowsdesktop-runtime-8.0.29-win-x64.exe -OutFile $installer
if((Get-AuthenticodeSignature $installer).Status -ne 'Valid'){throw 'Invalid Microsoft runtime installer signature'}