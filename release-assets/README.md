# Release assets

Run `scripts/Build-Release.ps1` from the repository root. Generated ZIP files and `SHA256SUMS.txt` are written to `release-assets/output/` and are intentionally excluded from Git.

- `LilithAI-vX.Y.Z-text.zip` contains the plugin and README.
- `LilithAI-vX.Y.Z-voice.zip` also contains the verified Chinese voice downloader.

The voice runtime is not nested inside the release ZIP. `Install-Chinese-Voice.ps1` downloads the original RC4 archives, verifies SHA-256, and extracts only the runtime, bundled .NET host, and reference WAV files used by LilithAI. The unused native voice pack and upstream plugin DLL are not installed.

`packages/` is an ignored local cache. `source/` preserves the upstream release manifests used to verify the source downloads.
