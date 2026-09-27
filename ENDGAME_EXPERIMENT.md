# Bounded endgame search

The player keeps the original networks and adds optional, deterministic
PIMC search near the end of a deal. It enumerates hands consistent with the deciding
seat's observations, solves each fully known ending by partnership alpha-beta, and
averages every legal action's game-point result. Future choices within each world
have perfect information; the average is an approximation to information-set values.

`UseEndgameSearch` is off by default, preserving existing callers. Its default horizon
is two tricks (at most 90 worlds). `EndgameTricks = 3` also solves three-trick endings
when at most eight worlds fit; otherwise it falls back to the existing network or
`SearchDeals` path. It gathers all worlds before solving, so overflow never produces
partially averaged action values. Public API additions are in ClaudePlayer only;
the engine and NuGet engine API are unchanged.

`EndgameUseDeclarations` optionally conditions worlds on the bots' observed policy
of declaring every available combination. Each hypothetical original hand is its
remaining cards plus public played cards. Its declaration types/counts must match
the observed declarations, as must any known ranks. Actual ranks in that hypothetical
hand resolve the announcement score. This is a policy assumption: a human who
withholds a declaration can make it inaccurate. If no worlds fit, normal evaluation
is used. A withheld own meld also causes a fallback, which matters when evaluating
a human's hint context. Without this option, unresolved announcement ranks cause a fallback.

## Validated fast configuration

Set `UseEndgameSearch = true`, `EndgameUseDeclarations = true`, `EndgameTricks = 3`
and `EndgameThreeTrickWorldLimit = 90`. The default constructor remains unchanged.
This configuration passed the independent promotion gate: **53.45% +/- .942 pp**
against ISMCTS at 100 ms/card over **2,000 games**, 95% interval
**[51.605%, 55.295%]**, +24 +/- 7 Elo. Against the frozen networks it scored
**54.65% +/- .168 pp over 20,000 games**, +32 +/- 1 Elo. Against SmartPlayer it
scored **89.74% +/- .206 pp over 20,000 games**, +377 +/- 4 Elo.

The latest integrated idle benchmark is **31.1 us/card** over 21,380 decisions
in 100 games, after 20 warmup games. The endgame path handled 4,996 decisions.
The unchanged four weight files total 2,974,830 bytes. Full results, variability
and reproduction commands are in [NEURAL_NETWORK.md](NEURAL_NETWORK.md#12-september-27-improvement-experiments).

The app uses this fast configuration for hints and for Expert (temperature 1.5,
MaxRegret 4). Master retains its 100-deal sampled search with a 400-ms budget:
it beat fast endgames at **53.1% +/- 1.255 pp over 1,000 games**, +22 +/- 9 Elo.
Adding endgames to that search scored **52.8% +/- 1.667 pp over 500 games** against
existing Master, 95% interval [49.532%, 56.068%]. This did not meet the predeclared
promotion threshold, so the combined profile remains unpromoted.

## Development measurements

Every strength row uses 20,000 whole games against frozen baseline weights,
mirrored pairs, seed 29, with one standard error across pairs:

| Variant | Win rate | Points/game | Elo |
|---|---|---|---|
| Two tricks, already-known announcement ranks only | 51.0% +/- .1 pp | +1.7 | +7 |
| Two tricks, observed declaration model | 52.3% +/- .1 pp | +3.6 | +16 |
| Three tricks with eight-world cap, observed declaration model | 53.0% +/- .1 pp | +4.5 | +21 |

Idle engine-card benchmark, 100 games after 20 warmup games:

| Variant | Mean us/card | Measured card decisions | Endgame evaluations |
|---|---|---|---|
| Frozen network | 19.2 | 21,336 | 0 |
| Two tricks plus declarations | 22.4 | 21,286 | 3,112 |
| Three tricks plus declarations | 25.0 | 21,391 | 3,612 |

The three-trick configuration meets the 50-us mean latency budget. The fixed
eight-world candidate scored 53.2% +/- .1 pp against the frozen network over
20,000 independent games and 52.4% +/- 1.4 pp against ISMCTS100 over 1,000 games
(+2.7 points/game, +17 Elo). The latter 95% interval includes 50%, so promotion
is refused. CLI seed 1000000007 maps to RNG seed 277147232 in the validator.
The neural weights themselves have not improved.

```powershell
dotnet artifacts/neural-20260927/endgame-three-bin/NeuralTrainer.dll validate --in artifacts/neural-20260927/baseline --opponent artifacts/neural-20260927/baseline --endgame true --endgame-declarations true --endgame-tricks 3 --pairs 10000 --threads 16 --seed 29
dotnet artifacts/neural-20260927/endgame-three-bin/NeuralTrainer.dll bench --in artifacts/neural-20260927/baseline --endgame true --endgame-declarations true --endgame-tricks 3
```

Verification: 94 C# tests pass after integration, Release build zero warnings/errors. Tests compare
alpha-beta against exhaustive play across all contracts, doubled/redoubled deals,
hanging points and both teams; compare world enumeration and every action value
against independent ternary assignments; check three-trick overflow fallback and
bounded settings; alter all secret hands without changing values; and verify exact
engine/seat-view parity including the 90-world profile and a withheld-own-meld hint
fallback. All inference is managed C#.

## Distillation experiment

`distill --teacher endgame --in <baseline> --endgame-declarations true
--endgame-tricks 3 --card-label-chance 1 --teacher-play-chance 0` records the
bounded endgame's values where it applies and the original network's values
elsewhere. It records every legal action and preserves bidding. Student
trajectories isolate target changes from changed play. Fitting 2.13 million positions
for 12 epochs, with two learning rates and extra identity-initialised depth, scored
only 50.3-50.4% +/- .2 pp against the original network over 20,000 games per student.
The neural weights remain unchanged.
Use separately seeded validation games and the GPU fitter's `--anchor-mean` to
preserve the original mean values while learning the teacher's action differences.

The optional `--endgame-worlds` setting bounds three-trick enumeration between 1
and 90 worlds (default 8); every valid two-trick world is still considered. Larger
limits require new strength and idle timing checks. For direct Master comparisons,
folder opponents accept `--opponent-search-deals`, `--opponent-search-milliseconds`
and `--opponent-endgame true`. The last option uses the candidate's declaration,
horizon and world-limit settings; all opponent options default to the old fast
network behavior. The full settings are printed with each validation result.

Match summaries now print the mirrored-pair standard error and normal 95% interval
to three decimal percentage points, plus the delta-method Elo standard error.
Tests cover interval precision near the promotion threshold and the Elo derivative.
This avoids making a promotion decision from a standard error rounded to one digit.
