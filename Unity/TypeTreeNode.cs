using System;

namespace Unity
{
    public unsafe partial class TypeTreeNode
    {
        readonly TypeTree owner;

        readonly ITypeTreeNodeImpl node;

        public string Name => owner.GetString(node.NameStrOffset);

        public TypeTreeNode(UnityVersion version, TypeTree owner, IntPtr address)
        {
            this.owner = owner;

            if (version >= UnityVersion.Unity2019_1)
                node = new V2(address);
            else
                node = new V1(address);
        }

        interface ITypeTreeNodeImpl
        {
            uint NameStrOffset { get; }
            ref byte GetPinnableReference();
        }
    }
}
