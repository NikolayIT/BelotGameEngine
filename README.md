# Belot Game Engine

Belot card game engine written in C#

Belot (Bridge-Belot or Belote) is a 32-card, trick-taking game popular in Bulgaria, France, Armenia, Croatia, Cyprus, Greece, Moldova, North Macedonia and also in Saudi Arabia.

## Engine package

```sh
dotnet add package BelotGameEngine --version 2.0.0
```

The NuGet package contains only the managed `Belot.Engine` library for .NET Standard 2.0,
with no bots, neural weights, UI or third-party runtime dependencies. See the
[engine README](src/Belot.Engine/README.md) for the `BelotMatch` API and migration from 1.1.
[Publishing instructions](tools/NuGetRelease/README.md) describe the trusted GitHub workflow.

## Play

`src/UI/Belot.UI` is a .NET MAUI app for Android and Windows, in English and Bulgarian: you play
South, and pick the level of your partner and of each rival, from a random player to the neural
player `ClaudePlayerNeural`. It keeps your rating, your game history and your record against each
level on the device.

## The AI players

- `SmartPlayer`: hand-written rules, improved change by change and measured in ELO.
- `ClaudePlayerIsmcts`: Information Set Monte Carlo Tree Search, +375 ELO over SmartPlayer.
- `ClaudePlayerNeural`: neural networks trained by reinforcement learning in self-play, which
  value every bid and every card. Alone they tie ClaudePlayerIsmcts at about 10 µs a decision;
  with a small search on top (`SearchDeals`) they beat it. See [NEURAL_NETWORK.md](NEURAL_NETWORK.md).
- The app's Master (`ClaudePlayerProfiles.CreateMaster`): the networks with search in every trick
  and a strong human player's technique and conventions (it bids naturally, keeps its high cards,
  signals to its partner and reads its partner's signals); it beats every other bot here. See
  [HUMAN_PLAY.md](HUMAN_PLAY.md).
- `ClaudePlayerHeuristic`: the written advice of the belot.bg academy and other Bulgarian sources
  as rules, no networks, with the last five tricks played out exactly over the deals the play
  allows (under 1 ms a card). It beats SmartPlayer 87.7% and the 2001 Belot 2.06 72.3%, and comes
  within 33 ELO of ClaudePlayerIsmcts. See [HEURISTIC_PLAYER.md](HEURISTIC_PLAYER.md).

## Build status

[![Build](https://github.com/NikolayIT/BelotGameEngine/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/NikolayIT/BelotGameEngine/actions/workflows/build.yml)
