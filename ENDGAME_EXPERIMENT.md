# Bounded endgame search (not promoted)

The experimental player keeps the original networks and adds optional, deterministic
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
is used. Without this option, unresolved announcement ranks cause a fallback.

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

Verification: 84 C# tests pass, Release build zero warnings/errors. Tests compare
alpha-beta against exhaustive play across all contracts, doubled/redoubled deals,
hanging points and both teams; compare world enumeration and every action value
against independent ternary assignments; check three-trick overflow fallback and
bounded settings; alter all secret hands without changing values; and verify exact
engine/seat-view parity with both endgame modes. All inference is managed C#.

## Distillation experiment

`distill --teacher endgame --in <baseline> --endgame-declarations true
--endgame-tricks 3 --card-label-chance 1 --teacher-play-chance 0` records the
bounded endgame's values where it applies and the original network's values
elsewhere. It records every legal action and preserves bidding. Student
trajectories isolate target changes from changed play. This tests whether the
endgame gain can be learned without runtime search; it is not yet a measured gain.
Use separately seeded validation games and the GPU fitter's `--anchor-mean` to
preserve the original mean values while learning the teacher's action differences.

The optional `--endgame-worlds` setting bounds three-trick enumeration between 1
and 90 worlds (default 8); every valid two-trick world is still considered. Larger
limits require new strength and idle timing checks. For direct Master comparisons,
folder opponents accept `--opponent-search-deals`, `--opponent-search-milliseconds`
and `--opponent-endgame true`. The last option uses the candidate's declaration,
horizon and world-limit settings; all opponent options default to the old fast
network behavior. The full settings are printed with each validation result.
