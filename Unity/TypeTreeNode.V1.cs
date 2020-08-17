using System;
using System.Runtime.CompilerServices;

namespace Unity
{
    public partial class TypeTreeNode
    {
        unsafe class V1 : ITypeTreeNodeImpl
        {
            public TypeTreeNode Node;

            public uint NameStrOffset => Node.NameStrOffset;

            public V1(IntPtr address)
            {
                if (address == IntPtr.Zero)
                    throw new ArgumentNullException(nameof(address));

                Node = *(TypeTreeNode*)address;
            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<TypeTreeNode, byte>(ref Node);
            }

            public struct TypeTreeNode
            {
                public short Version;
                public byte Level;
                public TypeFlags TypeFlags;
                public uint TypeStrOffset;
                public uint NameStrOffset;
                public int ByteSize;
                public int Index;
                public TransferMetaFlags MetaFlag;
            }
        }
    }
}
