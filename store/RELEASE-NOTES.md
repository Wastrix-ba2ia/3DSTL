# 商店版0.2.5.0更新内容

- 新增NAS网络路径预览缓存，减少未变化模型的重复读取和渲染；首次生成及匿名数据流仍可能需要完整读取。
- 新增文件夹预生成窗口，支持当前文件、完成/失败数量、错误原因、取消和强制更新；仅处理当前文件夹，不递归。
- 保留独立本地预览缓存与品牌图标；无常驻后台服务，不上传模型。
- 商店版由Microsoft Store更新；GitHub在线下载安装仅适用于传统桌面版。

# Store 0.2.5.0 release notes

- Added NAS metadata-based preview caching to reduce repeated reads of unchanged models. Initial renders and anonymous Shell streams may still require full reads.
- Added current-folder preview generation with progress, per-file errors, cancellation and force refresh. No recursive scanning.
- Retains bounded local preview caching and publisher brand icon. No resident service or model uploads.
- Store edition updates through Microsoft Store; GitHub updater is desktop-edition only.
