namespace Belot.AI.ClaudePlayer.Tests.Neural
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.NeuralTrainer;

    using Xunit;

    public class CardOwnershipTests
    {
        [Fact]
        public void OwnershipTargetsMatchTrueHandsWithoutChangingPolicyInputs()
        {
            var random = new Random(71);
            var simulator = new BelotSimulator();
            for (var kind = 0; kind < 6; kind++)
            {
                var deal = NeuralDeal.Deal(Enumerable.Range(0, 32).OrderBy(_ => random.Next()).ToArray(), kind % 4, 0);
                deal.Bid(FeatureEncoder.BidOfIndex(kind + 1));
                while (!deal.AuctionFinished)
                {
                    deal.Bid(FeatureEncoder.BidOfIndex(0));
                }

                deal.StartPlay(simulator, new DeclaredAnnounce[AnnounceScorer.MaxAnnounces]);
                while (!deal.IsFinished)
                {
                    deal.DeclareIfFirstCard();
                    var legal = simulator.LegalMoves(in deal.Play);
                    var indices = new int[FeatureEncoder.MaxActive];
                    var values = new float[FeatureEncoder.MaxActive];
                    var count = FeatureEncoder.EncodeCard(in deal, legal, indices, values);
                    var owners = CardOwnership.Encode(in deal);
                    for (var card = 0; card < 32; card++)
                    {
                        var rotated = FeatureEncoder.ToNetwork(card, FeatureEncoder.Rotation(kind));
                        var owner = (int)((owners >> (2 * rotated)) & 3);
                        var expected = 0;
                        for (var relative = 1; relative < 4; relative++)
                        {
                            if ((deal.Play.Hands[(deal.Play.Turn + relative) & 3] & (1u << card)) != 0)
                            {
                                expected = relative;
                            }
                        }

                        Assert.Equal(expected, owner);
                    }

                    // Removing hidden hands changes labels but cannot change the policy inputs.
                    var publicDeal = deal;
                    for (var relative = 1; relative < 4; relative++)
                    {
                        publicDeal.Play.Hands[(deal.Play.Turn + relative) & 3] = 0;
                    }

                    Assert.Equal(0UL, CardOwnership.Encode(in publicDeal));
                    var publicIndices = new int[FeatureEncoder.MaxActive];
                    var publicValues = new float[FeatureEncoder.MaxActive];
                    Assert.Equal(count, FeatureEncoder.EncodeCard(in publicDeal, legal, publicIndices, publicValues));
                    Assert.Equal(indices, publicIndices);
                    Assert.Equal(values, publicValues);
                    deal.PlayCard(simulator, BitOperations.TrailingZeroCount(legal), legal);
                }
            }
        }

        [Fact]
        public void OwnershipSidecarUsesTheSameRingSlotsAsTheSamples()
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "data.samples");
                var buffer = new SampleBuffer(2, 32, trackOwners: true);
                for (var sample = 1; sample <= 3; sample++)
                {
                    Assert.True(buffer.Add(new[] { sample }, new[] { 1f }, new float[32], 1, (ulong)sample));
                }

                buffer.Save(path);
                var loaded = SampleBuffer.Load(path);
                var batch = new Batch(2, 32);
                loaded.Take(batch, new[] { 0, 1 });
                Assert.Equal(3, batch.Indices[0]);
                Assert.Equal(2, batch.Indices[SampleBuffer.MaxFeatures]);
                using var reader = new BinaryReader(File.OpenRead(path + ".owners"));
                Assert.Equal(0x314F5042, reader.ReadInt32());
                Assert.Equal(FeatureEncoder.LayoutVersion, reader.ReadInt32());
                Assert.Equal(32, reader.ReadInt32());
                Assert.Equal(2, reader.ReadInt32());
                Assert.Equal(3UL, reader.ReadUInt64());
                Assert.Equal(2UL, reader.ReadUInt64());
                Assert.Equal(reader.BaseStream.Length, reader.BaseStream.Position);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
