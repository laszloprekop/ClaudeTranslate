# Every Lookup fetches the Prefetch Set; a stale Entry is replaced, never merged

A Lookup always asks for the whole Prefetch Set, whatever the user currently has enabled.
Enabling a language filters what is displayed rather than triggering a fetch. Each stored
Entry records its Coverage; opening a word whose Coverage is missing an Enabled Language
re-runs the whole Lookup and overwrites it.

Without this, changing the Enabled Languages leaves stored Entries stale in a way the
interface cannot explain — the Panel opens missing exactly the language the user just
switched on, with nothing on screen saying why.

## Considered options

Putting the language set in the storage key was rejected because it puts the same word on
the Word Trail twice, once per query variant, and the Trail is a list of words rather than
of queries. Clearing everything on a language change was rejected because it discards a
hundred paid Lookups over one toggle.

The interesting rejection is **topping up** — fetching just the newly-enabled language and
merging it into the stored Entry. That runs into Sense alignment: the stored Entry's Senses
are where *that* call drew the lines, and an independent call asking only for German has no
obligation to draw the same ones. It might return three Senses where the first returned
four, or split one in two. Merging correctly means sending the stored Senses back and
asking for Equivalents against exactly those — a second prompt, a second schema, partial
Entry states and a loading state inside an already-open Panel. Replacing costs one extra
Lookup for the single word being viewed, and keeps one prompt and one code path.

## Consequences

Every Lookup pays for the full Prefetch Set even when fewer languages are enabled, which is
why the Set is narrowable in settings rather than fixed. Toggling a language while the
Panel is open is instant, because the hidden languages were always there.

The store self-heals: words revisited after a language change are refetched with it,
words never reopened cost nothing.
