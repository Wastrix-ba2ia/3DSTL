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

## 在线更新 / Online updates

0.1.1 起，传统版默认每天启动时检查一次本仓库的正式 GitHub Release，有更新时后台下载，校验完成后点击“安装更新”。可以关闭自动检查/下载，也可以手动检查。更新过程写入 `%LOCALAPPDATA%/3DSTL/update.log`。0.1.0 需要先手动下载安装 0.1.1。

新版安装到 `%LOCALAPPDATA%/3DSTL/Versions/` 下独立的版本目录，并更新桌面快捷方式。原程序、语言设置、已启用格式、关联恢复备份保留；不强制重启 Explorer。缩略图组件如果仍被旧进程占用，重新登录 Windows 后完全切换。完整移除时也需移除保留的 Versions 目录。商店版继续使用 Microsoft Store 更新。

Desktop version 0.1.1 checks stable GitHub releases once daily on startup, downloads available updates and verifies SHA256. Click **Install update** when ready. You can disable automatic checks/downloads or check manually. Updates use a separate Versions directory, preserve settings and association-recovery data, and update the desktop shortcut. Old files are retained; sign out/in if Explorer still holds the old component. The Store edition remains updated by Microsoft Store.
