# A Material 3 custom colour role for warnings

False-friend Notes need a colour the palette does not have. The only non-text colour in the
app besides the accent is an error red, which is used when a Translation fails — a red box
on a *successful* Lookup reads as something having broken. The Note is the highest-value
content on screen and needs to stand out from the Equivalents rows directly above it.

We add a colour role following Material 3's shape — `warning`, `onWarning`,
`warningContainer`, `onWarningContainer` — derived per theme from an amber source. Material
3 has **no** warning role of its own: its semantic roles are primary, secondary, tertiary
and error. This is therefore a *custom* role, which M3 explicitly supports, rather than a
stock one.

Reusing `tertiary` was the alternative and was rejected: in M3 `tertiary` is defined as a
contrasting accent for balance, with no semantic meaning. Spending it on warnings overloads
it, and the day a genuine tertiary accent is wanted the token already means "careful".

## Consequences

This is the first Material 3 vocabulary in a palette that is otherwise hand-rolled, and it
is four tokens where every other colour comes in pairs. Warning is therefore either the odd
one out, or the first step toward restating the whole palette in M3 roles — that second
path is a project in its own right and is not implied by this decision.

The roles are also mirrored into the web stylesheet as unused custom properties. Nothing
consumes them today (see ADR-0005); they are there so the two front-ends do not diverge in
vocabulary before the web app ever needs them.
