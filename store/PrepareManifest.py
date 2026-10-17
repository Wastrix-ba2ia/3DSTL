from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import xml.etree.ElementTree as E,json
root=Path(__file__).resolve().parents[1];folder=root/'artifacts/store-payload';assets=folder/'Assets';assets.mkdir(exist_ok=True)
for size,name in [(44,'Square44.png'),(150,'Square150.png'),(50,'StoreLogo.png')]:
 image=Image.new('RGBA',(size,size),(27,41,60,255));d=ImageDraw.Draw(image);s=size/150
 d.polygon([(75*s,20*s),(128*s,48*s),(75*s,78*s),(22*s,48*s)],fill='#e4dfd0')
 d.polygon([(22*s,48*s),(75*s,78*s),(75*s,132*s),(22*s,101*s)],fill='#aaa99f')
 d.polygon([(75*s,78*s),(128*s,48*s),(128*s,101*s),(75*s,132*s)],fill='#d1cec1');image.save(assets/name)
identity=json.loads((root/'store/identity.json').read_text())
formats=['3mf','stl','obj','ply','off','amf','gcode','gco','step','stp','sldprt','sldasm','slddrw','prtdot','asmdot','drwdot','eprt','easm','edrw']
extensions=[];classes=[]
for n,ext in enumerate(formats):
 clsid=f'D3D51000-17A0-4D18-BA10-{n:012x}'
 extensions.append(f'<uap:Extension Category="windows.fileTypeAssociation"><uap:FileTypeAssociation Name="model-{ext}"><uap:DisplayName>3DSTL {ext.upper()} preview</uap:DisplayName><uap:SupportedFileTypes><uap:FileType>.{ext}</uap:FileType></uap:SupportedFileTypes><desktop2:ThumbnailHandler Clsid="{clsid}"/></uap:FileTypeAssociation></uap:Extension>')
 classes.append(f'<com:Class Id="{clsid}" Path="shell\\3DSTL.Thumbnail.dll" ThreadingModel="Both"/>')
manifest=f'''<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10" xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" xmlns:desktop2="http://schemas.microsoft.com/appx/manifest/desktop/windows10/2" xmlns:com="http://schemas.microsoft.com/appx/manifest/com/windows10" xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities" IgnorableNamespaces="uap desktop2 com rescap">
<Identity Name="{identity['Name']}" Publisher="{identity['Publisher']}" ProcessorArchitecture="x64" Version="0.2.0.0"/>
<Properties><DisplayName>3DSTL</DisplayName><PublisherDisplayName>{identity['PublisherDisplayName']}</PublisherDisplayName><Logo>Assets\\StoreLogo.png</Logo><Description>Local model thumbnails for Windows File Explorer</Description></Properties>
<Resources><Resource Language="zh-CN"/><Resource Language="en-US"/></Resources>
<Dependencies><TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.26100.0"/></Dependencies>
<Applications><Application Id="App" Executable="manager\\3DSTL.exe" EntryPoint="Windows.FullTrustApplication"><uap:VisualElements DisplayName="3DSTL" Description="Model thumbnails / 模型缩略图" Square150x150Logo="Assets\\Square150.png" Square44x44Logo="Assets\\Square44.png" BackgroundColor="#1B293C"/><Extensions>
{''.join(extensions)}
<com:Extension Category="windows.comServer"><com:ComServer><com:SurrogateServer DisplayName="3DSTL thumbnail host">{''.join(classes)}</com:SurrogateServer></com:ComServer></com:Extension>
</Extensions></Application></Applications><Capabilities><rescap:Capability Name="runFullTrust"/></Capabilities></Package>'''
E.fromstring(manifest)
(folder/'AppxManifest.xml').write_text(manifest,encoding='utf8');(root/'store/AppxManifest.xml').write_text(manifest,encoding='utf8')
print('Manifest matches Store identity; 19 thumbnail associations generated')