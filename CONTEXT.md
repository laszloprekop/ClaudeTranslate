# Translate

A personal multilingual translator and dictionary. The user types text in any language;
the app renders it into every language they have enabled, and — for a single word —
explains what that word means and how it is used.

## Language

### The two results

**Translation**:
A rendering of the user's input into one Enabled Language. Always produced for input of
two or more words.
_Avoid_: conversion, output

**Entry**:
A dictionary account of one Headword in one language — its pronunciation, part of speech,
and Senses. Produced only for single-word input.
_Avoid_: definition, dictionary result, article

**Lookup**:
The act of asking for Entries for a word. Triggered by typing one word, or by clicking any
word already on screen.
_Avoid_: search, query, define

### Inside an Entry

**Headword**:
The word an Entry is about, in its Lemma form.
_Avoid_: term, keyword, title

**Lemma**:
The dictionary form of a word — the form it is filed under. *gå* is the Lemma of *gick*;
*trög* is the Lemma of *trögare*.
_Avoid_: root, stem, base form

**Sense**:
One distinct meaning of a Headword, carrying its own gloss, Example, Equivalents,
Synonyms and Antonyms. A Headword has one Sense per meaning, ordered most common first.
_Avoid_: definition, meaning, usage

**Gloss**:
The prose explaining a Sense, written in the Headword's own language.
_Avoid_: definition, description

**Example**:
A short sentence showing a Sense in use, in the Headword's own language, with the
Headword marked inside it.
_Avoid_: usage, sample, citation

**Equivalent**:
A word in another Enabled Language that carries a particular Sense. Equivalents belong to
a Sense, never to a Headword as a whole — that distinction is the point of the feature.
_Avoid_: translation, rendering, counterpart

**False-friend Note**:
A warning attached to a Sense that a specific Equivalent-looking word in a named language
would be wrong here. Always names the language it concerns.
_Avoid_: warning, caveat, gotcha

**Synonym** / **Antonym**:
Words near or opposite a particular Sense, in the Headword's own language. Like
Equivalents, they belong to a Sense rather than to the Headword.

**Source Link**:
A link from an Entry out to a published dictionary for that Headword and language.
The app links; it never fetches.
_Avoid_: reference, citation, external source

### Languages

**Enabled Language**:
A language the user has switched on. Determines which Translations are produced and which
Equivalents are shown.
_Avoid_: target language, selected language, active language

**Prefetch Set**:
The languages every Lookup asks for, regardless of which are currently enabled. Defaults
to every language the app knows.
_Avoid_: fetch list, preload set

**Coverage**:
The languages a stored Entry actually contains. An Entry whose Coverage is missing an
Enabled Language is stale and is replaced on next open.
_Avoid_: completeness, freshness

### Where things live

**Panel**:
The column beside the app that shows the Entries for one word at a time.
_Avoid_: sidebar, drawer, flyout

**Translation Card**:
One stacked record of an input and its Translation into a single language.
_Avoid_: history item, result card, row

**Word Trail**:
The ordered list of words that have been looked up, newest first, capped at a fixed
length. It is both the visible history of Lookups and the store of their Entries — a word
on the Trail always has its Entries; a word that falls off the end loses them.
_Avoid_: history, cache, recents, breadcrumb

**Style Guide**:
The user's written instruction for how Translations should sound. Applies to Translations
only; Entries are not styled by it.
_Avoid_: tone, prompt, preferences
