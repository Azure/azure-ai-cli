using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    internal static class NativeUtils
    {
        public static NativeMemory ToNativeNullTerminatedUtf8String(string? str)
        {
            if (str == null)
            {
                return new NativeMemory(IntPtr.Zero, 0);
            }

            var utf8Bytes = Encoding.UTF8.GetBytes(str);
            var utf8LengthWithTerminator = utf8Bytes.Length + 1;
            var nativeHandle = IntPtr.Zero;

            try
            {
                nativeHandle = Marshal.AllocHGlobal(utf8LengthWithTerminator);
                Marshal.Copy(utf8Bytes, 0, nativeHandle, utf8LengthWithTerminator - 1);
                Marshal.WriteByte(nativeHandle, utf8LengthWithTerminator - 1, 0);
                return new NativeMemory(nativeHandle, utf8LengthWithTerminator);
            }
            catch
            {
                if (nativeHandle != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(nativeHandle);
                }
                throw;
            }
        }
    }
}