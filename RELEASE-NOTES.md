## 3DSTL v0.1.2

下载 `3DSTL-0.1.2-win-x64.zip`，完整解压并运行 `安装-Install.exe`。已有 0.1.1 可以在软件内检查更新、下载后点击“安装更新”。

- 大 3MF 改为流式读取：压缩输入上限 512 MiB，单个模型 XML 上限 1 GiB，总 XML 上限 2 GiB。单个网格超过 150 万面时扫描全部三角形，生成最多 40 万面的简化灰色显示网格。
- 较大 3MF 在桌面刷新队列中排在普通模型之后，低优先级处理，最多等待 180 秒。
- 自动尝试摆正方向明确、细长且有明显粗细端的单体；圆形头骨、宽面罩、方向不明及多零件组合保留原方向。不会保证所有耳朵或角都能摆正。
- 原始模型不修改、不上传；内置备用图片不旋转。旧缩略图需要刷新才会变化；Explorer 占用旧组件时可重新登录 Windows。

首次读取仍会较慢，也会使用临时磁盘。实测五个 NAS 3MF 单次几何渲染约 2～12 秒，进程峰值约 190～390 MiB（不含 Windows 多尺寸重复请求）。细节会简化；超出安全预算或解析失败时仍可能使用内置图片。OBJ 限制未调整，持久缓存及多尺寸渲染复用尚未实现。已提交认证的 Microsoft Store MSIX 未替换。

---

Version 0.1.2 streams large 3MF meshes and generates reduced gray previews instead of immediately falling back at 1.5 million faces. Limits: 512 MiB compressed input, 1 GiB per model XML and 2 GiB total XML. Larger 3MFs run after ordinary desktop-refresh jobs. Conservative automatic upright orientation applies to clearly elongated, tapered single meshes; ambiguous, round and multi-part shapes retain their orientation. Original files are unchanged. First-render latency, temporary disk usage and safety limits remain; embedded fallback images are not rotated. Existing 0.1.1 users can check for updates in the app. Verify the ZIP with SHA256SUMS.txt.
