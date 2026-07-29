# Senses come first; Equivalents hang off each Sense

A Lookup returns a list of Entries — one per Enabled Language in which the word exists —
and each Entry is a list of Senses, with Equivalents, Synonyms, Antonyms and any
False-friend Note attached to the individual Sense rather than to the Headword.

The alternative shape, and the obvious one for a translator, was a bilingual list: *inert →
trög, inaktiv, reaktionströg*. It was rejected because that list is exactly the mistake the
feature exists to prevent. Swedish uses *inert* for the chemistry sense and *trög* for the
figurative one; a flat list gives the learner three candidates and no way to choose, which
is worse than useless when the wrong choice is comic. Attaching Equivalents to Senses is
also what makes False-friend Notes possible at all — they can only be written by a model
holding every language at once.

## Consequences

**The top level is a list, not one Entry.** A single word carries no context, so detecting
one language is a coin flip: *gift* is a present in English and poison in Swedish, *man* is
a noun in English and a pronoun in Swedish. Returning every Entry that exists among the
Enabled Languages costs nothing for the common case — most words exist in one language and
yield one Entry — and pays precisely when there is a second meaning worth seeing.

**Entries are written in the Headword's own language.** The Gloss for *trög* is Swedish.
The learner is never stranded, because the Equivalents row on each Sense is already the
bridge out into their other languages.

**Senses are capped.** Beyond the cap the model is in archaic and hyper-technical
territory, where a generated Entry is least accurate and a learner has no way to tell.
The cap is a quality instruction as much as a limit.

**The schema needs a way to return nothing.** With structured outputs, a model asked for a
list of Entries and given no legitimate way to return zero will invent one — a confident,
well-formatted, entirely fabricated Entry for a typo. Zero Entries plus an optional note
and an optional suggested word covers misspellings, proper nouns and non-words as one
shape rather than three cases.
