using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ManagedTypeTree = Unity.TypeTree;

namespace Unity
{
    public partial class TypeTree
    {
        // Unity 2019.1 - 2019.2
        unsafe class V2019_1 : ITypeTreeImpl
        {
            internal TypeTree Tree;

            public IReadOnlyList<byte> StringBuffer => Tree.Data->StringBuffer;

            public IReadOnlyList<uint> ByteOffsets => Tree.Data->ByteOffsets;

            public IReadOnlyList<TypeTreeNode> Nodes => m_Nodes;

            private TypeTreeNode[] m_Nodes;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void TypeTreeDelegate(out TypeTree tree, MemLabelId* label, bool allocatePrivateData);

            public V2019_1(ManagedTypeTree owner, SymbolResolver resolver)
            {
                var constructor = resolver.ResolveFunction<TypeTreeDelegate>($"??0TypeTree@@Q{NameMangling.Ptr64}AA@A{NameMangling.Ptr64}BUMemLabelId@@_N@Z");
                var label = resolver.Resolve<MemLabelId>("?kMemTypeTree@@3UMemLabelId@@A");
                constructor.Invoke(out Tree, label, allocatePrivateData: false);
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
                public TypeTreeShareableData PrivateData;
            }

            internal struct TypeTreeShareableData
            {
                public DynamicArray<TypeTreeNode.V2019_1.TypeTreeNode> Nodes;
                public DynamicArray<byte> StringBuffer;
                public DynamicArray<uint> ByteOffsets;
                public TransferInstructionFlags FlagsAtGeneration;
                public int RefCount;
                public MemLabelId* MemLabel;
            }
        }
    }
}
