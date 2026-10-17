param([string]$Dotnet='dotnet',[string]$Python='python',[Parameter(Mandatory=$true)][string]$Zig,[Parameter(Mandatory=$true)][string]$MakeAppx)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$out=Join-Path $root 'artifacts\store-payload'
$version=(Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
$base=Join-Path $root "artifacts\3DSTL-$version-win-x64"
if(!(Test-Path (Join-Path $base 'cad-runtime\python.exe'))){throw 'Prepare the portable runtime first: scripts/Prepare-Runtime.ps1'}
& $Python (Join-Path $PSScriptRoot 'PrepareManager.py')
& $Dotnet publish (Join-Path $PSScriptRoot 'Worker.csproj') -c Release -o (Join-Path $out 'worker')
if($LASTEXITCODE -ne 0){throw 'Worker publish failed'}
& $Dotnet publish (Join-Path $PSScriptRoot 'Manager.csproj') -c Release -o (Join-Path $out 'manager')
if($LASTEXITCODE -ne 0){throw 'Manager publish failed'}
New-Item -ItemType Directory -Path (Join-Path $out 'shell') -Force | Out-Null
& $Zig c++ -target x86_64-windows-gnu -shared -O2 (Join-Path $PSScriptRoot 'NativeThumbnail.cpp') -o (Join-Path $out 'shell\3DSTL.Thumbnail.dll') -lole32 -luuid -lgdi32 -Wno-dll-attribute-on-redeclaration
if($LASTEXITCODE -ne 0){throw 'Native thumbnail build failed'}
Copy-Item (Join-Path $base 'cad-runtime') -Destination $out -Recurse -Force
Copy-Item (Join-Path $root 'src\ConvertMesh.py'),(Join-Path $root 'src\EmbeddedCadPreview.py'),(Join-Path $root 'LICENSE'),(Join-Path $root 'THIRD-PARTY-NOTICES.txt') -Destination $out -Force
Copy-Item (Join-Path $root 'docs\PRIVACY.md') -Destination (Join-Path $out 'PRIVACY.md') -Force
& $Python (Join-Path $PSScriptRoot 'PrepareManifest.py')
# Include upstream runtime and native-library notices in the payload before publishing.
& $Python (Join-Path $PSScriptRoot 'MakeMap.py')
& $MakeAppx pack /f (Join-Path $root 'artifacts\store-map.txt') /p (Join-Path $root 'artifacts\3DSTL-0.2.0.0-x64.msix') /o
if($LASTEXITCODE -ne 0){throw 'MSIX packaging or validation failed'}