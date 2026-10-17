# 3DSTL 商店提交资料 / Store listing draft

## 中文
名称：3DSTL
发布者显示名称：BA2IA（需与开发者后台允许的发布者身份对应）
简述：在 Windows 资源管理器和桌面直接查看 STL、3MF、STEP 等模型缩略图。
介绍：3DSTL 是免费的本地模型缩略图管理工具，提供浅灰立体预览、中文/English 界面、一键安装修复及恢复原关联。作者 BA2IA，QQ：21780，感谢 Codex。原创源码采用 MIT 许可，第三方组件遵循各自许可证。
格式：STL、3MF、OBJ、PLY、OFF、AMF、GCODE、GCO、STEP、STP；SLDPRT、SLDASM、SLDDRW、PRTDOT、ASMDOT、DRWDOT、EPRT、EASM、EDRW 仅尝试读取内置预览。
限制：特殊GCODE、过大模型、无内置图片或不兼容的专有CAD文件可能无法预览。专有格式兼容性未完整验证。
价格：免费。
建议分类：工具 / Utilities。
支持网址：https://github.com/Wastrix-ba2ia/3DSTL/issues
项目网址：https://github.com/Wastrix-ba2ia/3DSTL
截图：docs/screenshot-zh.png、docs/screenshot-en.png。

## English
Name: 3DSTL
Short description: Local model thumbnails for Windows File Explorer and your desktop.
Description: Free model thumbnail management with light-gray rendering, Chinese/English UI, Repair/Install, and previous-association recovery. Author BA2IA, QQ: 21780. Thanks to Codex. Original source is MIT; dependencies retain their licenses.
Formats: STL, 3MF, OBJ, PLY, OFF, AMF, GCODE, GCO, STEP, STP. SLDPRT, SLDASM, SLDDRW, PRTDOT, ASMDOT, DRWDOT, EPRT, EASM, EDRW: embedded-preview extraction only; compatibility is not guaranteed.
Price: Free.

## 提交状态（2026-10-17）
- Microsoft Store 应用 ID：`9NQ6SSHMXRQM`。商店版 MSIX 已上传并通过后台程序包验证；产品提交已进入预处理/认证流程，最终发布仍取决于微软审核。
- GitHub Release `v0.1.0` 提供传统桌面版 ZIP。ZIP 与商店 MSIX 是不同的发行包。
- 腾讯 PC 软件市场的官方公开提交入口尚未核实；腾讯云 IOA 软件仓库仅用于指定企业终端的内部软件分发。
- 360 软件管家要求软著，作者目前不打算办理软著。华军软件园尚未完成开发者账号注册及实名认证，未提交软件。

## 官方参考
https://learn.microsoft.com/en-us/windows/apps/publish/get-started
https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/publish-first-app
https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-desktop2-thumbnailhandler
