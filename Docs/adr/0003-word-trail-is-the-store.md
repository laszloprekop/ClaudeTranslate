# The Word Trail is the store, not a view over one

The list of looked-up words and the store of their Entries are one structure, not two. The
Word Trail holds a fixed number of entries, newest first; each holds a Lemma and the
Entries returned for it. A word falling off the end takes its Entries with it.

The obvious design is two things — a display list of recent words and a separate cache with
its own size limit. It was rejected because two lists drift apart: the Trail would show a
word whose Entries had been silently evicted, the user would click it, and they would pay
for the Lookup again with nothing on screen explaining why. One list gives the interface a
promise it can actually keep — **if it is on screen, it is stored**.

## Consequences

The store's size is chosen by what reads well in a collapsed one-line strip rather than by
memory pressure. At this length that is a few hundred kilobytes of settings, which is fine.

Words are stored under their Lemma, not under what was typed. Typing *gick*, *går* and
*gå* across a week is one row and two hits rather than three rows and three misses.

Because a looked-up word never produces a Translation Card, the Trail *is* the entire
history for single-word input. That raises its importance: it is not a convenience strip,
it is the only way back to a word you looked at yesterday.
