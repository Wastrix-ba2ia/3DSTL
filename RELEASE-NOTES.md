## 3DSTL v0.1.1

下载 `3DSTL-0.1.1-win-x64.zip`，完整解压并运行 `安装-Install.exe`。Windows 10/11 x64，免费，中英文界面。

- 新增 **GitHub 项目主页** 链接、**检查更新** 和 **自动检查并下载更新**。
- 启动时每天检查一次正式 Release；后台下载并验证 SHA256，完成后点击 **安装更新**。保留旧版、语言设置、已启用格式和关联恢复备份。
- 大 STL 支持提高至 1 GiB：完整扫描，生成减量显示网格；桌面刷新先处理普通模型，大 STL 排在末尾逐个处理。
- 修复按钮增加即时反馈、后台进度、失败汇总与日志入口。

**0.1.0 没有在线更新功能，需要先手动安装一次 0.1.1。** Explorer 如仍占用旧组件，重新登录 Windows 后完全切换。Microsoft Store 版继续由商店更新，本次没有替换已提交认证的 MSIX。

OBJ 仍有 128 MiB/150 万面限制；3MF 仍有 150 万面及每个模型 XML 180 MiB 限制，复杂文件可能使用内置预览。首次生成大模型仍可能较慢；渲染复用、多尺寸持久缓存、NAS 读取复用等性能优化尚未实现。模型原文件不会修改或上传。

---

Extract the complete ZIP and run `安装-Install.exe`. Version 0.1.1 adds a GitHub project link, manual update checks and optional automatic background downloads of stable releases, with SHA256 verification and versioned installation. Click **Install update** after download; previous files, language settings and association backups are retained. Version 0.1.0 requires one manual upgrade. Store updates remain managed by Microsoft Store.

Large STL previews (up to 1 GiB) and visible desktop-refresh progress are included. OBJ/3MF limits and first-render latency still apply; persistent render-cache and NAS optimizations are not yet implemented. SHA256SUMS.txt verifies the ZIP.
