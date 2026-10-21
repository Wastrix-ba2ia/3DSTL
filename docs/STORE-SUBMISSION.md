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

## 2026-10-21：本地文件打开修正（0.2.2.0）

新报告 c078f8c5-e44e-4266-832f-fc0213c49e11 指出打开模型后只有应用界面，没有模型预览。确认旧商店程序未处理文件参数，且没有应用内预览；此前只测Shell缩略图不足以覆盖此路径。

修正：uap3:FileTypeAssociation 明确声明 Parameters=--preview "%1"；StoreManager处理文件参数，ModelPreviewForm显示独立进程生成的图片预览。管理器提供“打开模型预览”，预览页有打开文件、拖放、内置示例、加载/错误提示及中英文切换。渲染可取消，最长180秒，关闭窗口取消任务，不改模型。产品不提供交互式旋转/编辑，商店介绍明确为图片预览。

验证：tests/ModelPreviewTest.csproj 检查实际显示STL/3MF/STEP、重复打开、缺失文件、错误语言切换、恢复及取消。实际MSIX在干净Windows11 x64沙盒安装；3种应用内预览成功；通过MSIX生成的AppX ProgID和ShellExecuteEx真正按文件关联打开普通及中文/空格STL文件，两次均确认模型渲染后的窗口标题（不是只启动EXE）。最初UWP ActivateForFile测试接口返回80270254，改用适用于打包桌面程序的实际Shell打开路径后通过。11个文件x3尺寸=33项Shell缩略图通过，随后卸载通过。测试沙盒按记录ID e5b84114-3c1b-4b8c-8d2c-9952b462f9f4 停止；CLI列表为空，vmmemWindowsSandbox退出。未实测ARM64硬件，不声称所有设备均已验证。

测试及截图在artifacts/certification-20261021；新包artifacts/3DSTL-0.2.2.0-x64.msix。中英文介绍和截图增加真实文件预览，其他测试信息更新为本轮问题根因及复测步骤。GitHub传统桌面版仍为0.1.2，没有混用商店版本号。
2026-10-21 已重新提交0.2.2.0，后台显示正在认证，处于预处理阶段。凭证：artifacts/certification-20261021/resubmitted.png。

## 2026-10-21：0.2.2.0再次被拒及0.2.3.0

报告ed7733e3-4193-4a44-ac01-b718705f97ec仍为10.1.2.10，原文再次描述打开本地模型后看不到缩略图。未提供模型、截图或错误。不能确认本次审核设备上的根因，也不能把x64本机通过等同于所有设备通过。

0.2.3.0将商店版开始菜单启动入口改为直接显示预览页并渲染内置sample.stl；打开模型为首个按钮，缩略图管理移到工具栏设置按钮。文件关联继续进入指定文件预览。失败时将worker异常保留在本地preview-error.log（64KiB后清理），不上传；临时worker错误文件用后清理。传统桌面版默认入口不变。

真实MSIX在离线Windows11 x64 Sandbox安装测试通过：shell:AppsFolder启动显示渲染后的sample.stl标题；普通及非ASCII/空格STL关联打开通过；STL/3MF/STEP预览、33次Shell缩略图、卸载通过。独立测试覆盖损坏文件诊断和失败恢复，以及8轮语言切换。ARM64未实测。测试进程按ID57a052cc-0b3a-4cc9-8e31-4a17c15e035b停止，CLI列表为空且VM进程已退出。证据目录artifacts/certification-20261021-r3。

新包3DSTL-0.2.3.0-x64.msix已上传Validated。审核说明诚实注明无法复现原始设备的问题，并请求若仍失败提供具体模型、打开路径、截图和架构；未声称已确认或彻底消除原始问题。GitHub桌面发行版本仍为0.1.3。

2026-10-21：0.2.3.0已重新提交，合作伙伴中心显示正在认证（预处理阶段）。凭证artifacts/certification-20261021-r3/resubmitted.png。尚未获批准。包SHA256：57053BB19574CF5620A6CF0EDE37ADB24B41EC40338DBBFF8D2CFA20A7BDCB8D。
