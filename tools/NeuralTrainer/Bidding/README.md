# Learning the heuristic player's bids from how they did

The heuristic player bids by `LearnedBidding` (`src/AI/Belot.AI.ClaudePlayer/Heuristic`): for each natural
bid it may make, a point count over its five cards and the auction (`BidFeatures`) gives the game points
the bid should bring over passing. These scripts fit that point count to measured outcomes; the player
itself runs only the C# code and the weights embedded in `BidModelWeights.cs`. HEURISTIC_PLAYER.md has
the method, the tables and the results. Python 3.12 with numpy and pandas (no GPU).

## 1. Measure the bids

`NeuralTrainer bidlab` plays first deals of matches, the subject team against an opponent, both ways round
on the same cards. At every bid of a subject seat the deal is also replayed from the start with each other
natural bid it could have made there (a suit with its jack or nine and another card, no trumps with an
ace, all trumps with a jack, a double), everybody deciding as usual afterwards, and played to the end.
`bidfeatures` adds the numbers the player counts.

```bash
dotnet build tools/NeuralTrainer/NeuralTrainer.csproj -c Release -o artifacts/bidding/bin
T="dotnet artifacts/bidding/bin/NeuralTrainer.dll"
# the subject (any heuristic profile, "@model.txt" bids by a model file), the opponents, deals both ways
$T bidlab --player heuristic --opponent belot206 --pairs 150000 --threads 18 --seed 9403 --data artifacts/bidding/run-b206
# a partner other than the subject's own profile: only the subject's bids are replayed
$T bidlab --player heuristic --lab-partner belot206 --opponent belot206 --pairs 100000 --threads 18 --seed 9406 --data artifacts/bidding/run-pb206
$T bidfeatures --data artifacts/bidding/run-b206
```

Rules-only play takes about 35 s for 300,000 deals on 18 threads; with the exact endgames about
25 times as long.

## 2. Fit, read the tables, embed

```bash
cd tools/NeuralTrainer/Bidding
python fit_bids.py --out ../../../artifacts/bidding/model.txt ../../../artifacts/bidding/run-b206 ...
python bid_tables.py made ../../../artifacts/bidding/run-b206 ...
python bid_tables.py holdings ../../../artifacts/bidding/run-b206 ...
python embed_model.py ../../../artifacts/bidding/model.txt "Fitted on ..."
python -m unittest discover -s . -v
```

`fit_bids.py` holds out 20% of the deals and prints the one-step improvement on them: the game points a
deal the model's bids gain over the bids that made the data, each decision changed with everything after
it as it was. It predicts the match results well, but only a match decides: compare the new model with
`arena` (`heuristic@model.txt` against `heuristic`), refit on data the new model made (policy iteration),
and judge against several opponents and partners, not only itself: a model fitted on its own games alone
learns to exploit its own habits (the second self-play model beat the first by 10 Elo and the original
bidding by only 6).
