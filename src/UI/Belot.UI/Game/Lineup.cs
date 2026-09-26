namespace Belot.UI.Game
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Players;

    /// <summary>
    /// The three computer seats of a game, each at its own level: the partner (North) and the two
    /// rivals, West on the person's left and East on the right.
    /// </summary>
    public sealed class Lineup
    {
        public Lineup(AiLevel partner, AiLevel west, AiLevel east)
        {
            this.Partner = partner ?? throw new ArgumentNullException(nameof(partner));
            this.West = west ?? throw new ArgumentNullException(nameof(west));
            this.East = east ?? throw new ArgumentNullException(nameof(east));
        }

        public AiLevel Partner { get; }

        public AiLevel West { get; }

        public AiLevel East { get; }

        /// <summary>Gets the rivals' rating as a pair: the average of the two.</summary>
        public int RivalsElo => (int)Math.Round((this.West.Elo + this.East.Elo) / 2.0, MidpointRounding.AwayFromZero);

        /// <summary>Gets the rivals' levels, each once (the same level on both sides is one).</summary>
        public IReadOnlyList<AiLevel> RivalLevels => new[] { this.West, this.East }.Distinct().ToArray();

        /// <summary>The level at a computer seat.</summary>
        public AiLevel LevelAt(PlayerPosition seat) => seat switch
        {
            PlayerPosition.North => this.Partner,
            PlayerPosition.West => this.West,
            PlayerPosition.East => this.East,
            _ => throw new ArgumentOutOfRangeException(nameof(seat), seat, "South is the person."),
        };

        /// <summary>A new player for a computer seat (see <see cref="GameSession"/>).</summary>
        public IPlayer CreatePlayer(PlayerPosition seat) => this.LevelAt(seat).CreatePlayer();
    }
}
