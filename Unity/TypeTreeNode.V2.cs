using System;
using System.Runtime.CompilerServices;

namespace Unity
{
    public partial class TypeTreeNode
    {
        // 2019.1+
        internal unsafe class V2 : ITypeTreeNodeImpl
        {
            internal TypeTreeNode Node;

            public short Version => Node.Version;

            public byte Level => Node.Level;

            public TypeFlags TypeFlags => Node.TypeFlags;

            public uint TypeStrOffset => Node.TypeStrOffset;

            public uint NameStrOffset => Node.NameStrOffset;

            public int ByteSize => Node.ByteSize;

            public int Index => Node.Index;

            public TransferMetaFlags MetaFlag => Node.MetaFlag;

            internal V2(TypeTreeNode node)
            {
                Node = node;
            }

            public V2(IntPtr address)
            {
                if (address == IntPtr.Zero)
                    throw new ArgumentNullException(nameof(address));

                Node = *(TypeTreeNode*)address;
            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<TypeTreeNode, byte>(ref Node);
            }

            internal struct TypeTreeNode
            {
                public short Version;
                public byte Level;
                public TypeFlags TypeFlags;
                public uint TypeStrOffset;
                public uint NameStrOffset;
                public int ByteSize;
                public int Index;
                public TransferMetaFlags MetaFlag;
                public ulong RefTypeHash;
            }
        }
    }
}
