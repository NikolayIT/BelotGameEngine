namespace Belot.NeuralTrainer
{
    using System;
    using System.Collections.Generic;

    using Belot.AI.ClaudePlayer;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Records the neural search's values for every legal card. The teacher samples unseen
    /// hands from the player's context, exactly as it does in the app; engine truth is never
    /// passed to the teacher or encoder. Unlabelled decisions use the fast student; labelled
    /// decisions can also follow the student, to collect labels on the student's trajectories.
    /// </summary>
    internal sealed class SearchDistillPlayer : IPlayer
    {
        private readonly ClaudePlayerNeural teacher;
        private readonly ClaudePlayerNeural student;
        private readonly SampleBuffer[] buffers;
        private readonly Random random;
        private readonly double labelChance;
        private readonly double teacherPlayChance;
        private readonly BelotSimulator simulator = new BelotSimulator();
        private readonly int[] indices = new int[FeatureEncoder.MaxActive];
        private readonly float[] values = new float[FeatureEncoder.MaxActive];
        private readonly float[] labels = new float[FeatureEncoder.CardOutputs];

        public SearchDistillPlayer(NeuralModels models, SampleBuffer[] buffers, TrainingSettings settings, int seed)
        {
            if (settings.SearchDeals <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings), "A neural search teacher needs --search-deals above zero.");
            }

            if (!double.IsFinite(settings.TeacherPlayChance) || settings.TeacherPlayChance < 0 || settings.TeacherPlayChance > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(settings), "Teacher play chance must be between zero and one.");
            }

            this.teacher = new ClaudePlayerNeural(models)
            {
                SearchDeals = settings.SearchDeals,
                Rng = new Random(seed),
            };
            this.student = new ClaudePlayerNeural(models);
            this.buffers = buffers;
            this.random = new Random(seed ^ 0x5EED);
            this.labelChance = settings.CardLabelChance;
            this.teacherPlayChance = settings.TeacherPlayChance;
        }

        public BidType GetBid(PlayerGetBidContext context) => this.student.GetBid(context);

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => context.AvailableAnnounces;

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            if (context.AvailableCardsToPlay.Count <= 1 || this.random.NextDouble() >= this.labelChance)
            {
                return this.student.PlayCard(context);
            }

            var scores = this.teacher.EvaluateCards(context);
            if (!NeuralDeal.FromPlayContext(context, this.simulator, out var deal))
            {
                throw new InvalidOperationException("Cannot encode a search-teacher decision.");
            }

            var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
            var rotation = FeatureEncoder.Rotation(deal.Kind);
            var mask = 0u;
            var chosen = scores[0].Card;
            Array.Clear(this.labels);
            foreach (var score in scores)
            {
                var output = FeatureEncoder.ToNetwork(score.Card.GetHashCode(), rotation);
                this.labels[output] = (float)(score.Value / NeuralEvaluator.ValueScale);
                mask |= 1u << output;
                if (score.Value == scores[0].Value && score.Card.GetHashCode() < chosen.GetHashCode())
                {
                    chosen = score.Card;
                }
            }

            var features = FeatureEncoder.EncodeCard(in deal, legal, this.indices, this.values);
            this.buffers[1 + FeatureEncoder.CardNetwork(deal.Kind)].Add(
                this.indices.AsSpan(0, features), this.values.AsSpan(0, features), this.labels, mask);
            return this.teacherPlayChance >= 1 || (this.teacherPlayChance > 0 && this.random.NextDouble() < this.teacherPlayChance)
                ? new PlayCardAction(chosen)
                : this.student.PlayCard(context);
        }

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
        {
        }

        public void EndOfRound(RoundResult roundResult)
        {
        }

        public void EndOfGame(GameResult gameResult)
        {
        }
    }
}
