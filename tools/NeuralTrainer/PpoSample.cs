namespace Belot.NeuralTrainer
{
    /// <summary>One on-policy decision; ownership is a separate, training-only critic input.</summary>
    internal sealed class PpoSample
    {
        public int Deal { get; set; }

        public byte Seat { get; set; }

        public int[] Indices { get; set; }

        public float[] Features { get; set; }

        public uint Mask { get; set; }

        public byte Action { get; set; }

        public float LogProbability { get; set; }

        public float Outcome { get; set; }

        public float OldValue { get; set; }

        public int Next { get; set; } = -1;

        public ulong Owners { get; set; }
    }
}
