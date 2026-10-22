# 3DSTL 隐私政策 / Privacy policy

更新日期 / Updated: 2026-10-22

## 中文
3DSTL 在本机读取用户浏览或选择的模型文件，生成缩略图。模型文件不会由本软件上传到服务器。无需注册本软件账号。

本软件不主动收集或发送姓名、邮箱、模型内容、使用统计或设备标识。语言选择和传统桌面版的关联恢复记录保存在本机用户目录。渲染使用临时文件，完成后尝试清理；异常退出时可能残留临时文件。

带独立预览缓存的版本会在本机用户目录 `3DSTL/PreviewCache` 保存模型的预览图片，以文件内容摘要识别，不上传。默认图片缓存上限256 MiB；写入新预览时淘汰超额和30天未使用的旧图片。缓存中包含模型外观，即使原模型删除也可能保留到被淘汰；用户可手动删除此目录，之后按需重建。

传统版 0.1.1 起，默认每天启动时检查 GitHub 正式版本，有更新时后台下载；可在界面关闭“自动检查并下载更新”。这些 HTTPS 请求向 GitHub 发送正常连接信息（如 IP 地址）及软件版本 User-Agent，不发送模型内容、模型路径、关联备份或设备标识。更新设置、安装包与更新日志保存在本机用户目录。商店版使用 Microsoft Store 更新。

Microsoft Store、Windows 和第三方运行环境可能按照各自隐私政策处理安装、更新、诊断或崩溃信息。用户主动访问项目网站、GitHub或联系作者时，相应平台会按其规则处理数据。

卸载商店版由 Windows 管理关联和应用文件。传统桌面版的恢复备份可能保留，便于恢复原设置。

联系作者：BA2IA，QQ：21780。问题反馈：https://github.com/Wastrix-ba2ia/3DSTL/issues

## English
3DSTL reads model files you browse or select locally to generate thumbnails. The application does not upload your models and does not require its own account.

The app does not intentionally collect or transmit names, email addresses, model contents, usage analytics or device identifiers. Language preferences and legacy association-recovery data are stored locally. Temporary render files are normally cleaned up after use; crashes may leave temporary files behind.

Builds with independent preview caching retain model images in the local user directory `3DSTL/PreviewCache`, keyed by a content digest and never uploaded. The default image budget is 256 MiB. Writing a new preview evicts excess images and images unused for 30 days. Images reveal model appearance and may remain after the source model is deleted until eviction. You can delete this directory manually; images are recreated on demand.

Starting with desktop version 0.1.1, the app checks GitHub stable releases at most once per day on startup and downloads available updates by default. You can disable automatic checks/downloads in the UI. These HTTPS requests expose normal connection information (such as IP address) and an app-version User-Agent to GitHub; they do not send model content, model paths, recovery data or device identifiers. Update settings, packages and logs stay in the local user directory. The Store edition uses Microsoft Store updates.

Microsoft Store, Windows and third-party runtimes may process installation, update, diagnostic or crash information under their own policies. Visiting GitHub or contacting the author uses those platforms' policies.

Windows manages Store package removal and its associations. Recovery data from the unpackaged desktop edition may remain for restoration.

Contact: BA2IA, QQ 21780. Support: https://github.com/Wastrix-ba2ia/3DSTL/issues
