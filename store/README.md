# Microsoft Store 提交 / Submission

商店 ID：9NQ6SSHMXRQM。包身份见 identity.json。

上传文件：artifacts/3DSTL-0.2.0.0-x64.msix。
这是未签名的商店提交包，不是供直接双击安装的侧载包；由商店在审核/分发流程中处理签名。

## 后台填写
1. 打开 3DSTL 产品 → 新建提交 → 程序包，上传 MSIX。
2. 价格选择免费；分类工具。简介和完整格式清单使用 docs/STORE-SUBMISSION.md。
3. 上传截图；中英文内容分别填写。功能限制需保留，专有格式仅内置预览。
4. 隐私政策 URL：https://github.com/Wastrix-ba2ia/3DSTL/blob/main/docs/PRIVACY.md
5. 支持 URL：https://github.com/Wastrix-ba2ia/3DSTL/issues
6. runFullTrust 说明：本应用作为桌面缩略图扩展读取用户本地模型，并启动本地独立渲染进程；不上传模型、不要求账号。独立进程用于运行 CAD 转换和保护 Explorer。
7. 完成内容分级及平台问卷，核对后台程序包验证结果后提交认证。

## 已验证 / Validated
- MakeAppx 语义/清单打包校验通过，没有禁用 validation。
- 原生 COM → 独立自包含渲染进程：实际 3MF / STEP 均返回 256x256 位图。
- 身份与后台提供的 Name/Publisher/PublisherDisplayName 匹配。
- 本地未侧载安装，不修改证书信任。尚未完成 Store 认证或干净虚拟机端到端安装测试。

## 商店版差异
缩略图由 MSIX 清单注册，卸载由 Windows 管理；界面不写传统文件关联注册表。
原生轻量 COM 接口通过独立的 .NET 自包含进程渲染，不要求系统预装 .NET。
Python/CAD 环境随包附带。现有传统版缩略图处理器可能优先，测试商店版前建议在传统版管理界面卸载缩略图功能。

## 构建
需要 .NET 8 SDK、Python 3.12（Pillow）、Zig 0.13、Windows SDK MakeAppx，准备好 portable runtime 后运行 Build-Store.ps1。
LLVM libc++ / libc++abi / libunwind 使用 Apache-2.0 with LLVM exceptions；发布时保留相关版权许可，当前包包含于 ThirdParty。