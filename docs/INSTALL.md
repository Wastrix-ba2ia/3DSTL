# 安装与恢复 / Installation and recovery

完整解压 ZIP，运行 **安装-Install.exe**，选择中文或 English，点击安装。程序安装到 `%LOCALAPPDATA%\3DSTL\App` 并建立桌面快捷方式。缺少 .NET 8 时启动包内微软签名的运行时安装程序，可能需要管理员授权。

安装默认启用十种公开格式。SOLIDWORKS/eDrawings 仅支持内置预览，在管理界面手动勾选，不强制替换已有官方处理器。

**修复**：勾选格式，点击“一键修复 / 安装”。

**卸载功能**：点击“卸载缩略图功能”恢复原关联，不删除模型文件。程序与备份会保留。

**完整移除**：先卸载缩略图功能，退出管理器并重新登录 Windows 释放组件，再移除 App 文件夹及桌面快捷方式。关联备份位于 `%LOCALAPPDATA%\3DSTL\associations.json`。

不显示时，使用大图标／超大图标，取消文件夹选项中的“始终显示图标，从不显示缩略图”。首次生成需要时间，极大文件可能超出支持限制。桌面缓存不更新时重新登录 Windows。

## English

Extract the full ZIP and run **安装-Install.exe**. Select English. Setup installs into `%LOCALAPPDATA%\3DSTL\App` and creates a desktop shortcut. If needed, the bundled Microsoft-signed .NET 8 Desktop Runtime installer requests administrator authorization.

Ten open-format handlers are enabled by default. Proprietary embedded-preview handlers are opt-in. Repair installs selected handlers; Uninstall thumbnails restores previous associations without changing model files. App files and recovery data remain.

For complete removal, uninstall thumbnail handlers first, exit the manager, sign out/in to release loaded components, then remove the App folder and shortcut. Recovery data stays outside the App folder.

Use Large/Extra large icons. First generation takes time; oversized files may not render.
