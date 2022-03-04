using System;
using System.Runtime.InteropServices;

namespace TypeTreeDumper
{
    public static class FallbackLoader
    {
        public const string CallbackAddressName = "CALLBACK_ADDRESS";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CallbackDelegate();

        public static void Initialize()
        {
            var address = new IntPtr(long.Parse(Environment.GetEnvironmentVariable(CallbackAddressName)));
            Marshal.GetDelegateForFunctionPointer(address, typeof(CallbackDelegate)).DynamicInvoke();
        }
    }
}
