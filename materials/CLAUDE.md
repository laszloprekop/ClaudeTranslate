# CLAUDE.md

Context for Claude Code working on this project. Read before making changes.

## What this is

A personal English ⇄ Swedish translator. Single-file web app, no build step. The user
types a phrase in either language, presses Enter, and it auto-detects the source language
and translates to the other — in the user's own writing style — by calling the Anthropic API.

The user is a frontend dev (14+ yrs) currently learning backend .NET; treat them as a strong
engineer. Communicates in Hungarian, Swedish, and English.

## Files

- `translate.html` — the entire app: HTML + CSS + vanilla JS in one file. No dependencies
  except two Google Fonts stylesheets (IBM Plex Sans, Material Symbols Outlined) loaded via CDN.
- `README.md` — human-facing quick start.

## How it works

- One textarea. `Enter` (without Shift) triggers translation; `Shift+Enter` inserts a newline.
- `translate(text)` builds a prompt instructing the model to (1) detect English vs Swedish,
  (2) translate to the other language following the user's style guide, (3) return **strict JSON**:
  `{"source","target","translation"}`. The style guide is injected from the "Writing style" setting.
- Model: `claude-sonnet-4-20250514`, `max_tokens: 1000`.
- Response parsing strips ```` ```json ```` fences and slices from first `{` to last `}` before
  `JSON.parse`, with a raw-text fallback. Keep this tolerance if you touch the prompt.

### Dual API mode (important)

The same file runs in two environments:

1. **Inside a Claude.ai artifact** — keyless `fetch` to `https://api.anthropic.com/v1/messages`;
   the sandbox proxy injects auth. No key needed.
2. **Standalone (saved file / hosted)** — the user pastes an API key in Settings. The request then
   adds headers: `x-api-key`, `anthropic-version: 2023-06-01`, and
   `anthropic-dangerous-direct-browser-access: true`.

Detection is implicit: if a key is present in the field, the keyed path is used.

### Storage shim

`Store` persists `tr:style`, `tr:key`, and `tr:history` (last 8). Order of preference:
`window.storage` (Claude artifact API) → `window.localStorage` (standalone) → in-memory.
**Do not** call `localStorage` unguarded — the `window.storage`-first check is what keeps the
file working if it's ever pasted back into a Claude artifact.

## Conventions & constraints

- Vanilla JS, no framework, no bundler. Keep it single-file and dependency-light unless a task
  explicitly says otherwise.
- Visual direction is "clean, minimal chrome": IBM Plex Sans, Material Symbols Outlined icons,
  a single accent (`--accent`, indigo), CSS custom properties, dark mode via
  `prefers-color-scheme`. Don't add visual noise.
- Accessibility floor: keyboard-operable, visible focus, `prefers-reduced-motion` respected.

## Security

The API key sits in the browser's local storage in plaintext — acceptable for personal local use
only. For any hosted or shared deployment, move the key behind a server proxy and delete the key
field from the UI. Do not commit a real key to the repo.

## Roadmap / open tasks

1. **PWA**: add `manifest.webmanifest`, app icons, and a service worker (offline shell) so it
   installs from a URL.
2. **Server proxy**: a tiny endpoint that injects the API key; point `fetch` at it and remove the
   key UI. Stack undecided — user is currently working in ASP.NET Core, so .NET is a natural fit;
   Node/Express also fine.
3. **Third language**: optionally add Hungarian to the rotation, or an explicit direction picker.
4. **Deploy**: target is Coolify on Hetzner, under a `dentaku.se` subdomain.

## Run & dev

No build. Open `translate.html` directly, or serve it:

```fish
python3 -m http.server 8000
# then open http://localhost:8000/translate.html
```

Environment: macOS (Apple Silicon), `fish` shell, VS Code / JetBrains Rider.

## Smoke test

- Input `Hej, hur mår du?` → expect English output, tag `sv → en`.
- Input `Can you send me the file tomorrow?` → expect Swedish output, tag `en → sv`.
- Toggle Settings → edit Writing style → confirm tone of next translation shifts accordingly.
