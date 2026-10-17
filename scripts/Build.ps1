param([string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$version=(Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
$out=Join-Path $root "artifacts\3DSTL-$version-win-x64"
& $Dotnet build (Join-Path $root 'src\Shell.csproj') -c Release -p:Version=$version -o (Join-Path $out 'shell')
if($LASTEXITCODE -ne 0){throw 'Shell build failed'}
& $Dotnet build (Join-Path $root 'src\Manager.csproj') -c Release -p:Version=$version -o (Join-Path $out 'manager')
if($LASTEXITCODE -ne 0){throw 'Manager build failed'}
& $Dotnet build (Join-Path $root 'src\ThumbnailRefresh.csproj') -c Release -p:Version=$version -o (Join-Path $out 'manager\refresh')
if($LASTEXITCODE -ne 0){throw 'Thumbnail refresher build failed'}
& $Dotnet build (Join-Path $root 'src\Updater.csproj') -c Release -p:Version=$version -o (Join-Path $out 'manager\updater')
if($LASTEXITCODE -ne 0){throw 'Updater build failed'}
@{product='3DSTL';version=$version;platform='win-x64'} | ConvertTo-Json | Set-Content (Join-Path $out 'release.json') -Encoding utf8
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:winexe /platform:x64 "/out:$out\安装-Install.exe" /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Microsoft.CSharp.dll (Join-Path $root 'src\Setup.cs')
if($LASTEXITCODE -ne 0){throw 'Installer build failed'}
Copy-Item (Join-Path $root 'src\ConvertMesh.py'),(Join-Path $root 'src\EmbeddedCadPreview.py'),(Join-Path $root 'LICENSE'),(Join-Path $root 'THIRD-PARTY-NOTICES.txt') -Destination $out -Force
Write-Output "Built $out. See Prepare-Runtime.ps1 to add portable dependencies."
