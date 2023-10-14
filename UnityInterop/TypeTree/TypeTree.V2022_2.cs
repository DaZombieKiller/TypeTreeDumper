using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ManagedTypeTree = Unity.TypeTree;

namespace Unity
{
    public partial class TypeTree
    {
        // Unity 2022.2+
        unsafe class V2022_2 : ITypeTreeImpl
        {
            internal TypeTree Tree;

            public IReadOnlyList<byte> StringBuffer => Tree.Data->StringBuffer;

            public IReadOnlyList<uint> ByteOffsets => Tree.Data->ByteOffsets;

            public IReadOnlyList<TypeTreeNode> Nodes => m_Nodes;

            private TypeTreeNode[] m_Nodes;

            public V2022_2(ManagedTypeTree owner, SymbolResolver resolver)
            {
                //var constructor = (delegate* unmanaged[Cdecl]<TypeTree*, MemLabelId*, void>)resolver.Resolve($"??0TypeTree@@Q{NameMangling.Ptr64}AA@A{NameMangling.Ptr64}BUMemLabelId@@@Z");
                //MemLabelId label = MemLabelId.DefaultMemTypeTree_2020_2_6;
                //TypeTree tree;
                //constructor(&tree, &label);
                //Tree = tree;

                //Strangely, the constructor seems unnecessary even though it's still present on this version.
                Tree = default;
            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<TypeTree, byte>(ref Tree);
            }

            public void CreateNodes(ManagedTypeTree owner)
            {
                var nodes = new TypeTreeNode[Tree.Data->Nodes.Size];

                for (int i = 0; i < nodes.Length; i++)
                    nodes[i] = new TypeTreeNode(new TypeTreeNode.V2019_1(Tree.Data->Nodes.Ptr[i]), owner);
                m_Nodes = nodes;
            }

            internal struct TypeTree
            {
                public TypeTreeShareableData* Data;
                public IntPtr ReferencedTypes;
                [MarshalAs(UnmanagedType.U1)]
                public bool PoolOwned;
            }

            internal struct TypeTreeShareableData
            {
                public DynamicArray<TypeTreeNode.V2019_1.TypeTreeNode, MemLabelId> Nodes;
                public DynamicArray<byte, MemLabelId> Levels;
                public DynamicArray<int, MemLabelId> NextIndex;
                public DynamicArray<byte, MemLabelId> StringBuffer;
                public DynamicArray<uint, MemLabelId> ByteOffsets;
                public TransferInstructionFlags FlagsAtGeneration;
                public int RefCount;
                public MemLabelId* MemLabel;
            }
        }
    }
}
