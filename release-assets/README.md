# Release assets

Run `scripts/Build-Release.ps1` from the repository root. Generated ZIP files and `SHA256SUMS.txt` are written to `release-assets/output/` and excluded from Git.

- `LilithAI-vX.Y.Z-text.zip` includes BepInEx and the plugin.
- `LilithAI-vX.Y.Z-text+voice.zip` includes BepInEx, the plugin, reference audio, and the complete Chinese voice runtime.

Both packages can be extracted directly beside `Lilith.exe`. The build excludes the upstream plugin DLL, unused native voice packs, local configuration, logs, and conversation data.

`packages/` is an ignored local source cache. `source/` preserves the upstream release manifests and checksums.
