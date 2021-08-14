using System;
using System.Runtime.CompilerServices;

namespace Unity
{
    public partial class TypeTreeNode
    {
        internal unsafe class V1 : ITypeTreeNodeImpl
        {
            public short Version { get; private set; }

            public byte Level { get; private set; }

            public TypeFlags TypeFlags { get; private set; }

            public uint TypeStrOffset { get; private set; }

            public uint NameStrOffset { get; private set; }

            public int ByteSize { get; private set; }

            public int Index { get; private set; }

            public TransferMetaFlags MetaFlag { get; private set; }

            internal V1(
                short version,
                byte level,
                TypeFlags typeFlags,
                uint typeStrOffset,
                uint nameStrOffset,
                int byteSize,
                int index,
                TransferMetaFlags metaFlag)
            {
                Version = version;
                Level = level;
                TypeFlags = typeFlags;
                TypeStrOffset = typeStrOffset;
                NameStrOffset = nameStrOffset;
                ByteSize = byteSize;
                Index = index;
                MetaFlag = metaFlag;
            }

            public ref byte GetPinnableReference()
            {
                throw new NotImplementedException();
            }
        }
    }
}
