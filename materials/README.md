# Translate — EN ⇄ SV

A tiny single-file translator. Type a phrase in English or Swedish, press **Enter**, and it
detects the language and translates to the other one in your writing style. Powered by the
Anthropic API.

## Quick start (standalone)

1. Open `translate.html` in a browser (double-click it).
2. Click the slider icon (top right) and paste an Anthropic API key from
   <https://console.anthropic.com>.
3. Type and press Enter. `Shift+Enter` makes a newline.

The key is stored only in your browser. This is fine for personal use on your own machine; for a
shared or hosted version, put the key behind a server proxy instead (see `CLAUDE.md`).

## Install as a desktop app (macOS)

Open the file in Chrome → ⋮ → **Cast, save, and share** → **Install page as app**. You get a Dock
icon and its own window.

## Develop

No build step. Serve locally with:

```fish
python3 -m http.server 8000
```

Then open <http://localhost:8000/translate.html>.

## Working with Claude Code

`CLAUDE.md` in this folder holds the project context, conventions, and roadmap. From this
directory, run `claude` and it will pick that up automatically.
