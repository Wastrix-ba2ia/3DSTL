# 3DSTL 商店提交资料 / Store listing draft

## 中文
名称：3DSTL
发布者显示名称：BA2IA（需与开发者后台允许的发布者身份对应）
简述：在 Windows 资源管理器和桌面直接查看 STL、3MF、STEP 等模型缩略图。
介绍：3DSTL 是免费的本地模型缩略图管理工具，提供浅灰立体预览、中文/English 界面、Windows 管理的缩略图关联及刷新通知。作者 BA2IA，QQ：21780，感谢 Codex。原创源码采用 MIT 许可，第三方组件遵循各自许可证。
格式：STL、3MF、OBJ、PLY、OFF、AMF、GCODE、GCO、STEP、STP；SLDPRT、SLDASM、SLDDRW、PRTDOT、ASMDOT、DRWDOT、EPRT、EASM、EDRW 仅尝试读取内置预览。
限制：特殊GCODE、过大模型、无内置图片或不兼容的专有CAD文件可能无法预览。专有格式兼容性未完整验证。
价格：免费。
建议分类：工具 / Utilities。
支持网址：https://github.com/Wastrix-ba2ia/3DSTL/issues
项目网址：https://github.com/Wastrix-ba2ia/3DSTL
截图：docs/store-screenshot-zh.png、docs/store-screenshot-en.png。

## English
Name: 3DSTL
Short description: Local model thumbnails for Windows File Explorer and your desktop.
Description: Free model thumbnail management with light-gray rendering, Chinese/English UI, Windows-managed thumbnail associations and refresh notifications. Author BA2IA, QQ: 21780. Thanks to Codex. Original source is MIT; dependencies retain their licenses.
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

## 2026-10-20 认证修正（0.2.1.0）

微软报告 25d25cdb-3a03-4d6e-9b41-b29453b0e08e 明确指出：切换 English 触发重复键异常，界面继续显示中文。实际重复键为“商店版由 Windows 管理格式关联；卸载时自动移除。”，由 PrepareManager.py 将原键替换后又插入相同键造成。已移除重复插入并在生成阶段检测重复翻译键，修正商店版卸载确认及刷新提示，隐藏不适用的传统版刷新日志和自动下载选项。

验证：tests/StoreLanguageTest.csproj 连续8轮真实语言菜单切换、控件/格式英文检查、语言设置保存及中英文重启通过。Windows Sandbox 中安装临时签名的同一0.2.1.0包，中英文启动截图通过；10种主要格式的Windows缩略图缓存请求（48/96/256）共30项成功，随后卸载通过。测试证书仅存在于已隔离沙盒，未修改宿主机证书信任。主机映射共享目录不支持所需操作锁，因此测试样本先复制到沙盒本地磁盘。专有格式仍仅尝试提取内置预览，未宣称完整兼容。

本次提交文件：artifacts/3DSTL-0.2.1.0-x64.msix。当前GitHub桌面版0.1.2与商店版本号独立。中英文商店介绍去掉传统版“修复关联”承诺；审核备注需明确英文异常的修复、复现验证步骤、runFullTrust 的独立本地渲染用途。

重新提交结果：2026-10-20 已在合作伙伴中心重新提交，页面明确显示“正在认证”。提交仍为 Submission 1，程序包已换为0.2.1.0并Validated。中英文介绍、截图、受限功能说明及其他测试信息均已保存。后台提示通常数小时，某些情况最多3个工作日；不能等同于已获批准。凭证：artifacts/certification-20261020/resubmitted.png。
