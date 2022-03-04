using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ManagedTypeTree = Unity.TypeTree;

namespace Unity
{
    public partial class TypeTree
    {
        // Unity 5.3 - 2018.4
        unsafe class V5_3 : ITypeTreeImpl
        {
            internal TypeTree Tree;

            public IReadOnlyList<byte> StringBuffer => Tree.StringBuffer;

            public IReadOnlyList<TypeTreeNode> Nodes => m_Nodes;

            public IReadOnlyList<uint> ByteOffsets => Tree.ByteOffsets;

            private TypeTreeNode[] m_Nodes;

            public V5_3(ManagedTypeTree owner, SymbolResolver resolver)
            {
                var constructor = (delegate* unmanaged[Cdecl]<TypeTree*, MemLabelId*, void>)resolver.Resolve($"??0TypeTree@@Q{NameMangling.Ptr64}AA@A{NameMangling.Ptr64}BUMemLabelId@@@Z");
                var label = resolver.Resolve<MemLabelId>("?kMemTypeTree@@3UMemLabelId@@A");
                TypeTree tree;
                constructor(&tree, label);
                Tree = tree;
            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<TypeTree, byte>(ref Tree);
            }

            public void CreateNodes(ManagedTypeTree owner)
            {
                var nodes = new TypeTreeNode[Tree.Nodes.Size];

                for (int i = 0; i < nodes.Length; i++)
                    nodes[i] = new TypeTreeNode(new TypeTreeNode.V5_0(Tree.Nodes.Ptr[i]), owner);

                m_Nodes = nodes;
            }

            internal struct TypeTree
            {
                public DynamicArray<TypeTreeNode.V5_0.TypeTreeNode> Nodes;
                public DynamicArray<byte> StringBuffer;
                public DynamicArray<uint> ByteOffsets;
            }
        }
    }
}
