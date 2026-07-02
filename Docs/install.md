# Installing the prebuilt apps

Every tagged version is published on the [Releases page](https://github.com/laszloprekop/ClaudeTranslate/releases).
The links below always fetch the **latest** release.

All apps need an [Anthropic API key](https://console.anthropic.com/): the desktop app asks for it
under **Settings** (the slider icon, top right); the web server reads the `ANTHROPIC_API_KEY`
environment variable.

## macOS

| Chip | Download |
| --- | --- |
| Apple Silicon | [Translate-osx-arm64.zip](https://github.com/laszloprekop/ClaudeTranslate/releases/latest/download/Translate-osx-arm64.zip) |
| Intel | [Translate-osx-x64.zip](https://github.com/laszloprekop/ClaudeTranslate/releases/latest/download/Translate-osx-x64.zip) |

Unzip and drag `Translate.app` to Applications. The app is signed ad-hoc (not notarized), so the
first launch will be blocked with a "damaged / unverified developer" warning. Clear it once with
either:

- **Right-click → Open → Open**, or
- `xattr -d com.apple.quarantine /Applications/Translate.app`

## Windows

Download [Translate-win-x64.zip](https://github.com/laszloprekop/ClaudeTranslate/releases/latest/download/Translate-win-x64.zip),
unzip anywhere, and run `Translator.Desktop.exe`. If SmartScreen objects, click
**More info → Run anyway** (the binaries are unsigned).

## Linux

Download [Translate-linux-x64.tar.gz](https://github.com/laszloprekop/ClaudeTranslate/releases/latest/download/Translate-linux-x64.tar.gz), then:

```bash
mkdir -p ~/translate && tar -xzf Translate-linux-x64.tar.gz -C ~/translate
~/translate/Translator.Desktop
```

Requires an X11/Wayland desktop; Avalonia's usual dependencies (`libX11`, `libICE`, `libSM`,
`fontconfig`) are present on mainstream distros.

## Web app (self-hosted)

A self-contained server — no .NET install needed. Pick your platform:
[linux-x64](https://github.com/laszloprekop/ClaudeTranslate/releases/latest/download/Translate-Web-linux-x64.tar.gz) ·
[osx-arm64](https://github.com/laszloprekop/ClaudeTranslate/releases/latest/download/Translate-Web-osx-arm64.zip) ·
[win-x64](https://github.com/laszloprekop/ClaudeTranslate/releases/latest/download/Translate-Web-win-x64.zip)

```bash
mkdir -p translate-web && tar -xzf Translate-Web-linux-x64.tar.gz -C translate-web  # or unzip
cd translate-web
export ANTHROPIC_API_KEY=sk-ant-...
./Translator.Web --urls http://localhost:5000
```

Open <http://localhost:5000>. The browser never sees the key — translation runs server-side.
Keep the server private (localhost or behind auth); anyone who can reach it can spend your
API credits.

## Versioning

Releases follow semantic version tags (`v1.2.3`). Pushing a tag builds and publishes all
platforms automatically via [GitHub Actions](../.github/workflows/release.yml):

```bash
git tag v1.2.3 && git push origin v1.2.3
```
