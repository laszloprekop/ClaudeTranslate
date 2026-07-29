# Dictionary Panel — implementation plan

**Status: designed, not started. No code has been written.**

Read first, in this order:

1. [CONTEXT.md](../CONTEXT.md) — the vocabulary. Entry, Sense, Equivalent, Lemma, Word
   Trail and Coverage all mean something specific here; the code should use these names.
2. [dictionary-panel.md](./dictionary-panel.md) — the settled design and every concrete
   number.
3. [adr/](./adr/) — six decisions and why. Read 0002 before touching the schema and 0003
   before touching storage.

Mock: **[Translate — Dictionary Panel](https://www.figma.com/design/qzKZ390nWqLUUktbtJfKYi/Translate---Dictionary-Panel)**
— light and dark frames plus a Notes column restating the decisions on canvas.

---

## The shape of the change

`Translator.Core` gains a second capability alongside translation. It is a **sibling of
translation, not a variant of it** — different prompt, different schema, different result
type, same client and the same one door out to Anthropic.

```
                        ┌── TranslateAsync ──→ TranslationResult   (2+ words)
Translator.Core ────────┤
                        └── LookUpAsync ─────→ LookupResult        (exactly 1 word)
```

`Translator.Desktop` draws both. `Translator.Web` exposes both as endpoints but draws only
translation (ADR-0005).

## Order of work

Each step should build and stay green on its own.

### 1. Core: the result types

`LookupResult` holds a list of `Entry` plus the not-found fields. `Entry` holds headword,
language, pronunciation, part of speech, an optional lemma note, and its `Sense` list.
`Sense` holds the domain label, gloss, example, equivalents, synonyms, antonyms and any
false-friend notes.

Two things that are easy to get wrong and expensive to fix later:

- **Equivalents and false-friend notes belong to a `Sense`, never to an `Entry`.** This is
  the whole feature (ADR-0002). A flat list on `Entry` silently destroys it.
- **A false-friend note must carry the language it is about.** Entries are fetched for the
  whole Prefetch Set and filtered on display (ADR-0004), so an untagged note will be shown
  to someone who does not have that language enabled.

### 2. Core: the schema and prompt

Mirror `TranslationSchema` / `PromptBuilder`. Constraints that are not optional:

- Cap senses at **4** in the schema, and say in the prompt that these are the four a
  learner most needs — it is a quality instruction, not only a limit.
- Give the model a legitimate way to **return zero entries**. Without it, structured
  outputs guarantee a fabricated entry for `asdfgh` that looks exactly as trustworthy as a
  real one.
- Ask for the **lemma** as headword, and for the "gick — past tense of gå" note when the
  input was inflected.
- Ask for one entry **per language in which the word exists**, among the Prefetch Set.

### 3. Core: the call

Extend `ITranslator` (or add a sibling interface — your call; the constraint is that
`Translator.Core` stays the only place that talks to Anthropic).

- **Raise `MaxTokens`.** It is 1000 today; a capped entry runs ~1200–1500. With structured
  outputs an overrun does not truncate gracefully — the JSON never closes and
  deserialisation throws. This is a certainty, not a risk.
- Move the default model to `claude-opus-5` in both `Translator.cs` and `AppSettings.cs`.

### 4. Web: the endpoint, and only the endpoint

`POST /api/define` next to `/api/translate`. Roughly ten lines. Do not touch
`wwwroot/index.html` (ADR-0005). Add a line to the README's parity table saying the
dictionary is desktop-only — it currently sells the two front-ends as equals, which this
change makes untrue.

### 5. Desktop: storage

The Word Trail is one list of 100 entries, each holding a lemma, its entries, and its
Coverage. It is the visible history *and* the store (ADR-0003) — do not build a separate
cache beside it.

Adding fields to `AppSettings` is backward-compatible; existing settings files deserialise
with defaults, so no migration is needed. Add the Prefetch Set here too.

### 6. Desktop: the panel

Widen the window to 1080 when open. Panel is 400, own scroll, hairline left divider. Build
it against the **dark** frame in the mock — that is the environment this is used in.

Needs its own **copy** affordance per equivalent; single-word input no longer produces a
card to copy from.

### 7. Desktop: clickable words

Every word in every translation card, every word in the trail strip, and every equivalent,
synonym and antonym inside an entry. Clicking carries the word's **own language** — so
clicking a Swedish equivalent opens a Swedish entry.

This is the largest and fiddliest piece; it is last on purpose. Everything before it is
useful without it.

### 8. Chrome: icons and the warning role

Phosphor replaces Material Symbols on the web and the Material path data in `App.axaml`.
Flags stay as emoji. Add the four M3 warning tokens to both theme dictionaries and mirror
them into the web stylesheet as unused custom properties.

### 9. Reseed the examples

Three of the six seeded examples are single words, which under the new trigger rule can no
longer produce a card — a user cannot reproduce what the demo shows. Replace with phrases.

## Traps worth knowing before you start

| Trap | Why it bites |
| --- | --- |
| `MaxTokens = 1000` | Guaranteed deserialisation failure on a full entry |
| Equivalents attached to `Entry` | Silently destroys the feature's whole point |
| Untagged false-friend notes | Shows a German warning to someone with only Swedish on |
| No zero-entry path in the schema | The model fabricates entries for typos |
| A cache separate from the trail | The two drift; the UI shows words whose entries are gone |
| Six chips on one row | They wrap to two at 680 — the mock is optimistic here |
| Emoji flags ≠ Phosphor | Phosphor ships no country flags. Flags stay emoji, deliberately |

## Explicitly out of scope

- **Web UI for the dictionary.** ADR-0005.
- **Window vibrancy / native chrome.** Deferred, with reasoning in
  [dictionary-panel.md](./dictionary-panel.md#deferred). Avalonia cannot do macOS Liquid
  Glass at all.
- **Restating the palette in Material 3 roles.** ADR-0006 adds one custom role; it does not
  imply the rest.
