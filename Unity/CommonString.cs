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
            if (resolver.TryResolve("?BufferBegin@CommonString@Unity@@3QEBDEB", out IntPtr begin))
                BufferBegin = Marshal.ReadIntPtr(begin);

            if (resolver.TryResolve("?BufferEnd@CommonString@Unity@@3QEBDEB", out IntPtr end))
                BufferEnd = Marshal.ReadIntPtr(end);
        }
    }
}
