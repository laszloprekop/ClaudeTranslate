# Lookup ships in Core and the web API, but only the desktop app draws it

`Translator.Core` gains the whole Lookup capability and `Translator.Web` exposes it as an
endpoint, but the browser page is unchanged. The Panel, the Word Trail, clickable words and
the Prefetch Set are desktop-only.

The blocking reason is storage, not effort: the desktop persists to a settings file in the
OS app-data directory, while the web app holds nothing per user — its only state is a
server-side API key. The Word Trail *is* the store (ADR-0003), so building the Panel for
the web means first inventing a persistence story for it. The secondary reason is that the
browser front-end is a single hand-written HTML file that reimplements the desktop UI in
plain JavaScript; every decision recorded in these ADRs would need a second, independent
implementation with its own bugs.

## Consequences

**The two front-ends are no longer at parity**, and the README currently sells them as
equals. That table needs a line saying so.

The endpoint costs almost nothing and is worth keeping even with no UI behind it: it keeps
the "one shared core" claim true, so Core holds the entire capability and the web app
simply chooses not to draw it. Any other client can use it.
