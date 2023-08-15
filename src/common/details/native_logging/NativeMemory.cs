using System;
using System.Runtime.InteropServices;

namespace Azure.AI.Details.Common.CLI
{
    internal class NativeMemory : DisposableBase
    {
        public NativeMemory(IntPtr handle, int size)
        {
            Handle = handle;
            Size = size;
        }

        public IntPtr Handle { get; }

        public int Size { get; }

        protected override void Dispose(bool disposeManaged)
        {
            if (Handle != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Handle);
            }
        }
    }
}