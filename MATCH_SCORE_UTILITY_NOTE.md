# Future experiment: match-score-aware endings

The current endgame solver maximizes the deal's point difference. It does not
use the public cumulative match scores. This can favor a capot that continues
the match over a smaller noncapot score that wins it immediately. No utility
change is part of the selected belief5-v1 or belief5-v2-ensemble profiles.

A bounded follow-up would use terminal utility 1 for a legal match win, 0 for
a legal loss, and a calibrated continuation win probability otherwise. Match
termination must follow `BelotMatch.FinishRound`: at least 151 points, a strict
lead, positive points earned in that deal, and no capot. The continuation state
contains both cumulative scores, new hanging points and the next starting team.
All are public. Apply the utility at minimax leaves before averaging worlds.

Estimate continuation probabilities from actual match continuations or an
offline dynamic program over empirical deal transitions, retaining capot,
passed-out deals, zero awards and hanging points. A small managed helper or
compact table can supply cached terminal-outcome values. A finite-horizon
dynamic program can also track unresolved probability mass. Validate on
separate games and deliberately cover scores above 151 and hanging situations.
The PPO recorder's simplified reset when either team reaches 151 is unsuitable
for these continuation labels.

Keep existing `EvaluateCards`, `Temperature` and `MaxRegret` in game-point units.
Start with an internal deterministic experiment using separately named match
probabilities. Test every engine termination edge, public-view parity, a real
ending where conceding one trick closes the match, and transposition/budget
behavior. Re-measure latency because changing utility can change alpha-beta
pruning and the number of completed worlds. Promote only through whole-match
comparisons; a closing bonus or prediction improvement alone is insufficient.
