# Release assets

Run `scripts/Build-Release.ps1` from the repository root. Generated ZIP files and `SHA256SUMS.txt` are written to `release-assets/output/` and excluded from Git.

- `LilithAI-vX.Y.Z-base.zip` includes BepInEx, the plugin, and both reference voices.
- `LilithAI-vX.Y.Z-voice-chinese.zip` adds the complete Chinese GPT-SoVITS runtime.
- Every `LilithAI-vX.Y.Z-voice-japanese-*.zip` together adds the complete Japanese Irodori runtime and downloaded model.

All packages are additive and can be extracted directly beside `Lilith.exe`. Use base only, base + Chinese, base + all Japanese parts, or all packages. The build excludes the upstream plugin DLL, unused native voice packs, local configuration, logs, and conversation data.

`packages/` is an ignored local source cache. `source/` preserves the upstream release manifests and checksums.
