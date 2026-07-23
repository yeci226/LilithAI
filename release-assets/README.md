# Release assets

Run `scripts/Build-Release.ps1` from the repository root. It builds only the text package by default. Add `-IncludeVoice` only when the voice runtimes actually change. Generated ZIP files and `SHA256SUMS.txt` are written to `release-assets/output/` and excluded from Git.

- `LilithAI-vX.Y.Z-text.zip` includes BepInEx and the plugin, but no voice runtime.
- `LilithAI-vX.Y.Z-voice-chinese.zip` adds the complete Chinese GPT-SoVITS runtime.
- Every `LilithAI-vX.Y.Z-voice-japanese-*.zip` together adds the complete Japanese Irodori runtime and downloaded model.

All packages are additive and can be extracted directly beside `Lilith.exe`. Install the latest text package, then add the unchanged voice packages from v0.10.20 if wanted. The build excludes the upstream plugin DLL, unused native voice packs, local configuration, logs, and conversation data.

`packages/` is an ignored local source cache. `source/` preserves the upstream release manifests and checksums.
