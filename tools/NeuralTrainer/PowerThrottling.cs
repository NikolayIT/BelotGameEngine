namespace Belot.NeuralTrainer
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Keeps Windows from treating the trainer as background work: a console process without
    /// the focus may be throttled (EcoQoS: low clocks, efficiency cores), which slowed training
    /// runs several times over, unpredictably.
    /// </summary>
    internal static class PowerThrottling
    {
        private const int ProcessPowerThrottling = 4;
        private const uint ExecutionSpeed = 1;
        private const uint IgnoreTimerResolution = 4;

        public static void Disable()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var state = new PowerThrottlingState
            {
                Version = 1,
                ControlMask = ExecutionSpeed | IgnoreTimerResolution,
                StateMask = 0,
            };
            using var process = Process.GetCurrentProcess();
            if (!NativeMethods.SetProcessInformation(process.Handle, ProcessPowerThrottling, ref state, (uint)Marshal.SizeOf<PowerThrottlingState>()))
            {
                Console.WriteLine($"Could not turn off power throttling (error {Marshal.GetLastWin32Error()}).");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PowerThrottlingState
        {
            public uint Version;
            public uint ControlMask;
            public uint StateMask;
        }
    }
}
