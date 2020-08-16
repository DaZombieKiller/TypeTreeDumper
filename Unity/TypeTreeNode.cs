using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public unsafe class TypeTreeNode
    {
        TypeTreeNodeUnion* union;

        UnityVersion version;

        TypeTree owner;

        public TypeTreeNode(UnityVersion version, TypeTree owner, IntPtr address)
        {
            this.owner   = owner;
            this.version = version;
            union        = (TypeTreeNodeUnion*)address;
        }

        public string Name
        {
            get
            {
                if (version >= UnityVersion.Unity2019_1)
                    return owner.GetString(union->V2.NameStrOffset);
                else
                    return owner.GetString(union->V1.NameStrOffset);
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        struct TypeTreeNodeUnion
        {
            [FieldOffset(0)]
            public TypeTreeNodeV1 V1;

            [FieldOffset(0)]
            public TypeTreeNodeV2 V2;
        }

        struct TypeTreeNodeV1
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

        // 2019.1+
        struct TypeTreeNodeV2
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
