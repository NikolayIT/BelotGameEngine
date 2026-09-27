# Belot Game Engine

Belot card game engine written in C#

Belot (Bridge-Belot or Belote) is a 32-card, trick-taking game popular in Bulgaria, France, Armenia, Croatia, Cyprus, Greece, Moldova, North Macedonia and also in Saudi Arabia.

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

## Build status

[![Build](https://github.com/NikolayIT/BelotGameEngine/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/NikolayIT/BelotGameEngine/actions/workflows/build.yml)
