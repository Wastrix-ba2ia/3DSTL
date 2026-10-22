# 独立预览缓存（2026-10-22）

Windows缩略图缓存被清理后，3DSTL可从自己的PNG缓存恢复预览，避免重新解析模型。此机制位于MeshFormats.Render和Thumbnail.RenderOrEmbedded，覆盖传统COM处理器和独立渲染worker。首次生成仍需要完整读取模型。

- 目录：`%LOCALAPPDATA%\3DSTL\PreviewCache`，只存预览图片，不保存模型副本，不上传。
- 标识：渲染修订号、扩展名和文件内容SHA256。相同内容在随机临时路径/重命名后也能命中；同大小、同修改时间但内容变化仍失效。渲染期间内容变化则不写入旧标识。
- PNG保存采用同目录临时文件后原子替换；损坏图片视为未命中；无法保存缓存时仍可生成预览。
- 相同内容使用命名Mutex协调请求，避免并发重复渲染；进程退出不留驻留服务。等待超过150秒报繁忙错误。
- 默认256 MiB预算，写入新缓存时按最近使用时间清理超额和30天未使用图片，并清理超过一天的写入残留。命中只读取PNG并更新时间，不反复解析几何。内容摘要仍要顺序读模型，故不承诺零IO。
- 用户可删除此目录，之后按需重建。清理工具若连此目录也清理，仍需重新渲染。原模型删除后图片可能保留至淘汰，详见隐私政策。
- 修改几何解析、方向、材质或渲染外观时必须更新PreviewCache.Revision，以免复用旧图。

验证：tests/PreviewCacheTest.csproj涵盖内容变更、异名同内容、损坏回退、不可写目录、并发仅一次渲染、渲染期间变更和磁盘淘汰。STL/3MF独立worker实际显示通过。final/worker测试目录没有转换器及CAD运行时，仍成功显示已缓存的大OBJ，确认命中绕过转换。

桌面真实文件实测：面罩OBJ首次20.920秒、命中0.241秒；HEAD SKETCH 8.obj首次15.611秒、命中0.249秒，冷/热输出BMP摘要一致。最终COM处理器在WTS_FORCEEXTRACTION（绕过Windows缓存）下，6个OBJ全部成功，每个256像素请求0.484～0.774秒。证据artifacts/preview-cache。

本机已将原有启用格式重绑定到`%LOCALAPPDATA%\3DSTL\Versions\local-preview-cache-r2-20261022`，原绑定记录artifacts/preview-cache/previous-bindings.json；使用新目录避免覆盖已加载DLL。未修改默认打开程序，未运行管家、未清空Windows缓存、未启动沙箱或后台监控。GitHub公开下载包仍为v0.1.3，已提交认证的Store0.2.3.0不包含此后续功能。
