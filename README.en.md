# LilithAI

[繁體中文](README.md) · [简体中文](README.zh-CN.md) · **English** · [日本語](README.ja.md)

<p align="center">
  <img src="docs/images/hero.png" width="620" alt="LilithAI chat input">
</p>

Spend a little more time talking with Lilith in *The NOexistenceN of Lilith*.

LilithAI adds AI chat to the game's existing interaction menu, remembers recent conversation context, and lets replies trigger Lilith's expressions and actions. It supports OpenAI, Anthropic, Gemini, xAI, DeepSeek, Mistral, OpenRouter, and local Ollama or LM Studio servers.

> This is an unofficial mod and is not affiliated with the game's developer or publisher.

## Screenshots

| Chat | Settings |
| --- | --- |
| ![Chat with Lilith](docs/images/chat.png) | ![Lilith AI settings](docs/images/settings.png) |

## Download

Choose one package from [Releases](../../releases/latest):

| Package | Includes |
| --- | --- |
| `LilithAI-vX.Y.Z-text.zip` | Text chat only; no extra model or GPU required |
| `LilithAI-vX.Y.Z-text+voice.zip` | Text chat and local Chinese speech; about 2 GB, with 8 GB VRAM and 16 GB RAM recommended |

Both packages already include the required BepInEx files. There is nothing else to download.

## Installation

1. Close the game.
2. Download either the text or text + voice package.
3. **Extract** the ZIP into the Steam game directory—the folder containing `Lilith.exe`.
4. Start the game. The first launch takes longer while BepInEx creates its files.
5. Open the `Lilith AI` tab in the game settings, choose a provider and model, then enter your API key. For the voice package, set Voice to `Chinese`.

The default Steam path is usually:

```text
C:\Program Files (x86)\Steam\steamapps\common\The NOexistenceN of Lilith
```

After extraction, the game directory should contain:

```text
Lilith.exe
winhttp.dll
BepInEx\plugins\LilithAI.dll
```

To update, close the game and overwrite the old files with the new ZIP. Your settings and conversation history are not removed.

## Notes

- Your API key is stored only in `BepInEx/config/tw.shawn.lilith.ai.cfg`.
- Conversation history is stored in `BepInEx/data/LilithAI/memory.json`.
- `BepInEx/LogOutput.log` may contain conversation text. Review it before sharing a bug report.
- The player name is not sent by default. You can enable it with `Context.IncludePlayerName`.
- Dynamic Japanese speech requires a separate [Irodori TTS Server](https://github.com/Aratako/Irodori-TTS-Server) installation.

## Build from source

```powershell
dotnet build .\LilithAI.sln -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\The NOexistenceN of Lilith"
$env:DOTNET_ROLL_FORWARD='Major'
dotnet run --project .\tests\LilithAISmoke.csproj -c Release --no-build
.\scripts\Build-Release.ps1
```

Release packages are written to `release-assets/output/`.

## Credits

- [BepInEx](https://github.com/BepInEx/BepInEx)
- Chinese GPT-SoVITS runtime and reference material from [Lilith-AI-Mod](https://github.com/mimimi6666/Lilith-AI-Mod)
- [Irodori TTS Server](https://github.com/Aratako/Irodori-TTS-Server)

Game names, characters, and assets belong to their respective owners.
