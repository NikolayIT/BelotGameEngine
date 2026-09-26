namespace Belot.NeuralTrainer
{
    using System;
    using System.Runtime.InteropServices;

    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetProcessInformation(IntPtr process, int informationClass, ref PowerThrottling.PowerThrottlingState information, uint size);
    }
}
