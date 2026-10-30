# 3DSTL v0.1.5 · 2026-10-30

## 更新内容
- **NAS 预览加速**：新增网络路径缓存索引，文件大小、修改时间未变化时复用本机图片，减少重复读取和渲染。Windows 仅提供匿名数据流时仍保留完整读取回退，首次生成依然可能较慢。
- **文件夹预生成与进度**：在管理器点击“文件夹预生成”，选择目录后按文件大小排队，显示当前文件、完成数量和失败原因；支持取消与强制更新。仅处理当前目录，不递归，不在每个文件图标上显示动态进度。
- **独立本地缓存**：默认256 MiB上限，按需淘汰旧图片，无常驻后台进程。NAS文件若内容变了但大小、修改时间均未变化，请使用“强制更新”。
- **GitHub 在线升级**：桌面版可自动检查和下载正式版本，通过SHA256校验后点击“安装更新”；新版本使用独立目录，失败保留旧版。
- **品牌图标**：使用作者提供的黑白标志，升级后更新桌面快捷方式图标。

## 安装与更新
下载 Windows x64 ZIP，解压后运行“安装-Install.exe”。已有桌面版也可在3DSTL中点击“检查更新”，下载校验完成后点击“安装更新”。

微软商店版与桌面版分别发布：商店版由Microsoft Store更新，不通过GitHub替换商店安装文件。

## English
- NAS metadata-based preview cache reduces repeated model reads; anonymous Shell streams still use the full-read fallback.
- Cancellable folder preview generation with current-file progress and failure details; current folder only, no recursion. Force refresh handles unchanged metadata.
- Bounded local image cache (256 MiB by default), no resident background service.
- Desktop GitHub stable-release download, SHA256 verification and version-directory upgrade with previous-version preservation.
- Publisher-provided brand icon and updated shortcut icon.
- Microsoft Store installations update through Microsoft Store separately.
