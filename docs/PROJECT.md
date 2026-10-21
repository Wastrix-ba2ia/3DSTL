# 3DSTL 项目结构

3DSTL 是独立的 Windows 模型缩略图项目，GitHub 仓库：<https://github.com/Wastrix-ba2ia/3DSTL>。

| 路径 | 用途 |
| --- | --- |
| `src/` | 传统桌面版管理界面、缩略图处理器和模型渲染源码 |
| `store/` | Microsoft Store 版的 MSIX 清单、原生处理器和独立渲染进程 |
| `scripts/` | 传统版构建及运行时准备 |
| `tests/` | 格式样本生成及缩略图验证 |
| `docs/` | 安装、隐私、商店提交和项目说明 |
| `artifacts/` | 本机构建产物与打包依赖；不提交到 Git |

在本目录运行构建脚本。脚本使用自身所在目录寻找源码及输出路径，不需要副屏项目，也不读取旧的 `ThreeMfPreview` 试验目录。旧目录保留作为历史测试记录；后续修改、打包和发布以这里的源码为准。

公开 Windows 压缩包为 `artifacts/3DSTL-0.1.3-win-x64.zip`，对应 GitHub Release `v0.1.3`。Microsoft Store 的 MSIX 单独位于 `artifacts/3DSTL-0.2.0.0-x64.msix`，不能当作传统安装包直接双击安装。
