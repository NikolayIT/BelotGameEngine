# Belot Game Engine

Belot card game engine written in C#

Belot (Bridge-Belot or Belote) is a 32-card, trick-taking game popular in Bulgaria, France, Armenia, Croatia, Cyprus, Greece, Moldova, North Macedonia and also in Saudi Arabia.

## Build status

[![Build Status](https://nikolayit.visualstudio.com/BelotGameEngine/_apis/build/status/NikolayIT.BelotGameEngine?branchName=master)](https://nikolayit.visualstudio.com/BelotGameEngine/_build/latest?definitionId=17&branchName=master)

## Projects

- **Belot.Engine** - Core game engine library
- **Belot.AI.DummyPlayer** - Simple AI player implementation
- **Belot.AI.SmartPlayer** - Advanced AI player implementation
- **Belot.UI.Console** - Console-based UI for playing Belot
- **Belot.UI.Windows** - Windows desktop application
- **Belot.UI.Web** - ASP.NET Core web application with SignalR for online multiplayer (NEW!)

## Web Application

The project now includes a web application that allows 4 players to play Belot online using SignalR for real-time communication.

### Running the Web Application

```bash
cd src/UI/Belot.UI.Web
dotnet run
```

Then open http://localhost:5000 in multiple browser tabs to simulate multiple players.

See [src/UI/Belot.UI.Web/README.md](src/UI/Belot.UI.Web/README.md) for more details.
