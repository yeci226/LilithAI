# LilithAI

[繁體中文](README.md) · **简体中文** · [English](README.en.md) · [日本語](README.ja.md)

<p align="center">
  <img src="docs/images/hero.png" width="620" alt="LilithAI 对话输入画面">
</p>

让《The NOexistenceN of Lilith》里的莉莉丝陪你多聊一会儿。

LilithAI 将 AI 对话接入游戏原本的互动菜单，保留最近的聊天记录，也能让回复带动莉莉丝的表情与动作。支持 OpenAI、Anthropic、Gemini、xAI、DeepSeek、Mistral、OpenRouter，以及本地运行的 Ollama、LM Studio。

> 这是非官方模组，与游戏开发者及发行商无关。

## 画面

| 对话 | 设置 |
| --- | --- |
| ![莉莉丝 AI 对话](docs/images/chat.png) | ![Lilith AI 设置](docs/images/settings.png) |

## 下载

在 [Releases](../../releases/latest) 选择一个版本：

| 安装包 | 内容 |
| --- | --- |
| `LilithAI-vX.Y.Z-text.zip` | 纯文字聊天，不需要额外模型或 GPU |
| `LilithAI-vX.Y.Z-text+voice.zip` | 文字聊天＋中文本地语音，约 2 GB，建议 8 GB 显存、16 GB 内存 |

两个安装包都已经包含所需的 BepInEx，不必另外下载。

## 安装

1. 关闭游戏。
2. 下载纯文字版或文字＋语音版。
3. 将 ZIP **解压**到 Steam 游戏安装目录，也就是 `Lilith.exe` 所在的文件夹。
4. 启动游戏。第一次启动需要生成 BepInEx 文件，等待时间会比平时长。
5. 进入游戏设置的 `Lilith AI` 页面，选择服务、模型并填写 API key。语音版请将语音切换为 `中文`。

默认 Steam 路径通常是：

```text
C:\Program Files (x86)\Steam\steamapps\common\The NOexistenceN of Lilith
```

解压后应该能在游戏目录看到：

```text
Lilith.exe
winhttp.dll
BepInEx\plugins\LilithAI.dll
```

更新时关闭游戏，再用新版 ZIP 覆盖即可。设置和对话记录不会被安装包删除。

## 使用说明

- API key 只保存在 `BepInEx/config/tw.shawn.lilith.ai.cfg`。
- 对话记录保存在 `BepInEx/data/LilithAI/memory.json`。
- `BepInEx/LogOutput.log` 可能包含聊天内容；反馈问题前请先检查。
- 玩家名称默认不会发送给模型，可在 `Context.IncludePlayerName` 中自行开启。
- 日语动态语音需要另外安装 [Irodori TTS Server](https://github.com/Aratako/Irodori-TTS-Server)。

## 自行构建

```powershell
dotnet build .\LilithAI.sln -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\The NOexistenceN of Lilith"
$env:DOTNET_ROLL_FORWARD='Major'
dotnet run --project .\tests\LilithAISmoke.csproj -c Release --no-build
.\scripts\Build-Release.ps1
```

发布包会生成在 `release-assets/output/`。

## 致谢

- [BepInEx](https://github.com/BepInEx/BepInEx)
- [Lilith-AI-Mod](https://github.com/mimimi6666/Lilith-AI-Mod) 提供的中文 GPT-SoVITS runtime 与参考素材
- [Irodori TTS Server](https://github.com/Aratako/Irodori-TTS-Server)

游戏名称、角色与素材版权归原权利人所有。
