using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public class CommonString
    {
        public IntPtr BufferBegin { get; }

        public IntPtr BufferEnd { get; }

        public CommonString(SymbolResolver resolver)
        {
            var begin = resolver.Resolve("?BufferBegin@CommonString@Unity@@3QEBDEB");
            var end   = resolver.Resolve("?BufferEnd@CommonString@Unity@@3QEBDEB");

            if (begin != IntPtr.Zero)
                BufferBegin = Marshal.ReadIntPtr(begin);

            if (end != IntPtr.Zero)
                BufferEnd = Marshal.ReadIntPtr(end);
        }
    }
}
