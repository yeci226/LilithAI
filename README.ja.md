# LilithAI

[繁體中文](README.md) · [简体中文](README.zh-CN.md) · [English](README.en.md) · **日本語**

<p align="center">
  <img src="docs/images/hero.png" width="620" alt="LilithAI の会話入力画面">
</p>

『The NOexistenceN of Lilith』のリリスと、もう少しだけ会話を楽しめる Mod です。

LilithAI はゲーム本来のインタラクションメニューに AI 会話を追加します。直近の会話を記憶し、返答に合わせてリリスの表情やアクションも変化します。OpenAI、Anthropic、Gemini、xAI、DeepSeek、Mistral、OpenRouter のほか、ローカルの Ollama と LM Studio に対応しています。

> 非公式 Mod です。ゲームの開発元および販売元とは関係ありません。

## スクリーンショット

| 会話 | 設定 |
| --- | --- |
| ![リリスとの AI 会話](docs/images/chat.png) | ![Lilith AI の設定](docs/images/settings.png) |

## ダウンロード

[Releases](../../releases/latest) から、どちらか一つを選んでください。

| パッケージ | 内容 |
| --- | --- |
| `LilithAI-vX.Y.Z-text.zip` | テキスト会話のみ。追加モデルや GPU は不要です |
| `LilithAI-vX.Y.Z-text+voice.zip` | テキスト会話＋ローカル中国語音声。約 2 GB、VRAM 8 GB・RAM 16 GB 推奨です |

必要な BepInEx ファイルは両方のパッケージに含まれています。別途ダウンロードする必要はありません。

## インストール

1. ゲームを終了します。
2. テキスト版、またはテキスト＋音声版をダウンロードします。
3. ZIP を Steam のゲームフォルダー、つまり `Lilith.exe` がある場所へ**展開**します。
4. ゲームを起動します。初回のみ BepInEx のファイル生成が行われるため、通常より時間がかかります。
5. ゲーム設定の `Lilith AI` タブでプロバイダーとモデルを選び、API key を入力します。音声版では Voice を `Chinese` に設定してください。

通常の Steam インストール先：

```text
C:\Program Files (x86)\Steam\steamapps\common\The NOexistenceN of Lilith
```

展開後、ゲームフォルダーに次のファイルがあれば正しく配置されています。

```text
Lilith.exe
winhttp.dll
BepInEx\plugins\LilithAI.dll
```

更新時はゲームを終了し、新しい ZIP で上書きしてください。設定と会話履歴は削除されません。

## 補足

- API key は `BepInEx/config/tw.shawn.lilith.ai.cfg` にのみ保存されます。
- 会話履歴は `BepInEx/data/LilithAI/memory.json` に保存されます。
- `BepInEx/LogOutput.log` には会話内容が含まれる場合があります。不具合報告に添付する前に確認してください。
- プレイヤー名は初期設定では送信されません。`Context.IncludePlayerName` で有効にできます。
- 日本語の動的音声には [Irodori TTS Server](https://github.com/Aratako/Irodori-TTS-Server) の追加インストールが必要です。

## ビルド

```powershell
dotnet build .\LilithAI.sln -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\The NOexistenceN of Lilith"
$env:DOTNET_ROLL_FORWARD='Major'
dotnet run --project .\tests\LilithAISmoke.csproj -c Release --no-build
.\scripts\Build-Release.ps1
```

パッケージは `release-assets/output/` に生成されます。

## クレジット

- [BepInEx](https://github.com/BepInEx/BepInEx)
- [Lilith-AI-Mod](https://github.com/mimimi6666/Lilith-AI-Mod) の中国語 GPT-SoVITS runtime および参照素材
- [Irodori TTS Server](https://github.com/Aratako/Irodori-TTS-Server)

ゲーム名、キャラクター、素材の権利は各権利者に帰属します。
