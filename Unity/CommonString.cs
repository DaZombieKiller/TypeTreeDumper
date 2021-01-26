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
            if (resolver.TryResolve($"?BufferBegin@CommonString@Unity@@3Q{NameMangling.Ptr64}BD{NameMangling.Ptr64}B", out IntPtr begin))
                BufferBegin = Marshal.ReadIntPtr(begin);

            if (resolver.TryResolve($"?BufferEnd@CommonString@Unity@@3Q{NameMangling.Ptr64}BD{NameMangling.Ptr64}B", out IntPtr end))
                BufferEnd = Marshal.ReadIntPtr(end);
        }
    }
}
