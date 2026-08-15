namespace BelotV2
{
    /// <summary>
    /// Bit-exact port of Delphi's Random generator as used by belot.exe.
    ///
    /// Recovered from FUN_00402b78 (RE):
    ///     RandSeed := RandSeed * $08088405 + 1;
    ///     Result   := (Int64(Range) * RandSeed) shr 32;
    /// RandSeed is the 32-bit global at DAT_0048b040. This is the standard
    /// Delphi 5/6 linear-congruential RNG; reproducing it lets a reconstructed
    /// deal match the original given the same seed.
    /// </summary>
    public sealed class DelphiRandom
    {
        private uint seed;

        public DelphiRandom(uint seed) => this.seed = seed;

        public uint Seed
        {
            get => this.seed;
            set => this.seed = value;
        }

        /// <summary>Delphi Random(range): returns an int in [0, range).</summary>
        public int Next(int range)
        {
            // seed advances even when range <= 0 in Delphi; mirror that.
            this.seed = (this.seed * 0x08088405u) + 1u;
            if (range <= 0)
            {
                return 0;
            }

            return (int)(((ulong)(uint)range * this.seed) >> 32);
        }

        /// <summary>Delphi Random: real in [0,1). Not needed by the engine but kept for parity.</summary>
        public double NextDouble()
        {
            this.seed = (this.seed * 0x08088405u) + 1u;
            return this.seed * 2.3283064365386963e-10; // seed / 2^32
        }
    }
}
