using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public unsafe class CommonString
    {
        public sbyte* BufferBegin { get; }

        public sbyte* BufferEnd { get; }

        public CommonString(SymbolResolver resolver)
        {
            if (resolver.TryResolve($"?BufferBegin@CommonString@Unity@@3Q{NameMangling.Ptr64}BD{NameMangling.Ptr64}B", out void* begin))
                BufferBegin = *(sbyte**)begin;

            if (resolver.TryResolve($"?BufferEnd@CommonString@Unity@@3Q{NameMangling.Ptr64}BD{NameMangling.Ptr64}B", out void* end))
                BufferEnd = *(sbyte**)end;
        }

        public unsafe ReadOnlySpan<byte> GetData()
        {
            if (BufferBegin == null || BufferEnd == null)
                return ReadOnlySpan<byte>.Empty;

            var source = (byte*)BufferBegin;
            var length = (byte*)BufferEnd - source - 1;
            return new ReadOnlySpan<byte>(source, (int)length);
        }
    }
}
