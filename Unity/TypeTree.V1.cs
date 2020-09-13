using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ManagedTypeTree = Unity.TypeTree;

namespace Unity
{
    public partial class TypeTree
    {
        unsafe class V1 : ITypeTreeImpl
        {
            internal TypeTree Tree;

            public DynamicArray<byte> StringBuffer => Tree.StringBuffer;

            public IReadOnlyList<TypeTreeNode> Nodes => m_Nodes;

            private TypeTreeNode[] m_Nodes;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void TypeTreeDelegate(out TypeTree tree, MemLabelId* label);

            public V1(ManagedTypeTree owner, SymbolResolver resolver)
            {
                var constructor = resolver.ResolveFunction<TypeTreeDelegate>("??0TypeTree@@QEAA@AEBUMemLabelId@@@Z");
                var label = resolver.Resolve<MemLabelId>("?kMemTypeTree@@3UMemLabelId@@A");
                constructor.Invoke(out Tree, label);
            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<TypeTree, byte>(ref Tree);
            }

            public void CreateNodes(ManagedTypeTree owner)
            {
                var nodes = new TypeTreeNode[Tree.Nodes.Size];

                for (int i = 0; i < nodes.Length; i++)
                    nodes[i] = new TypeTreeNode(new TypeTreeNode.V1(Tree.Nodes.Ptr[i]), owner);

                m_Nodes = nodes;
            }

            internal struct TypeTree
            {
                public DynamicArray<TypeTreeNode.V1.TypeTreeNode> Nodes;
                public DynamicArray<byte> StringBuffer;
                public DynamicArray<uint> ByteOffsets;
            }
        }
    }
}
