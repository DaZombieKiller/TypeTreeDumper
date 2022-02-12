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

        public unsafe byte[] GetData()
        {
            if (BufferBegin == IntPtr.Zero || BufferEnd == IntPtr.Zero)
                return Array.Empty<byte>();

            var source = (byte*)BufferBegin;
            var length = (byte*)BufferEnd - source - 1;
            var buffer = new byte[length];

            fixed (byte* destination = buffer)
                Buffer.MemoryCopy(source, destination, length, length);

            return buffer;
        }
    }
}
