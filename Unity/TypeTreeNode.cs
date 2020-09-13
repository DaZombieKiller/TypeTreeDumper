using System;

namespace Unity
{
    public unsafe partial class TypeTreeNode
    {
        readonly TypeTree owner;

        readonly ITypeTreeNodeImpl node;


        public short Version => node.Version;

        public byte Level => node.Level;

        public TypeFlags TypeFlags => node.TypeFlags;

        public uint TypeStrOffset => node.TypeStrOffset;

        public uint NameStrOffset => node.NameStrOffset;

        public string TypeName => owner.GetString(node.TypeStrOffset);

        public string Name => owner.GetString(node.NameStrOffset);

        public int ByteSize => node.ByteSize;

        public int Index => node.Index;

        public TransferMetaFlags MetaFlag => node.MetaFlag;

        internal TypeTreeNode(ITypeTreeNodeImpl impl, TypeTree owner)
        {
            this.owner = owner;
            node       = impl;
        }

        public TypeTreeNode(UnityVersion version, TypeTree owner, IntPtr address)
        {
            this.owner = owner;

            if (version >= UnityVersion.Unity2019_1)
                node = new V2(address);
            else
                node = new V1(address);
        }

        internal interface ITypeTreeNodeImpl
        {
            public short Version { get; }
            public byte Level { get; }
            public TypeFlags TypeFlags { get; }
            public uint TypeStrOffset { get; }
            public uint NameStrOffset { get; }
            public int ByteSize { get; }
            public int Index { get; }
            public TransferMetaFlags MetaFlag { get;  }
            ref byte GetPinnableReference();
        }
    }
}
