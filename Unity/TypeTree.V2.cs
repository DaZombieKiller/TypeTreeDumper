using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ManagedTypeTree = Unity.TypeTree;

namespace Unity
{
    public partial class TypeTree
    {
        // Unity 2019.1 - 2019.2
        unsafe class V2 : ITypeTreeImpl
        {
            internal TypeTree Tree;

            public DynamicArray<byte> StringBuffer => Tree.Data->StringBuffer;

            public IReadOnlyList<TypeTreeNode> Nodes { get; }

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void TypeTreeDelegate(out TypeTree tree, MemLabelId* label, bool allocatePrivateData);

            public V2(ManagedTypeTree owner, SymbolResolver resolver)
            {
                var constructor = resolver.ResolveFunction<TypeTreeDelegate>("??0TypeTree@@QEAA@AEBUMemLabelId@@_N@Z");
                var label       = resolver.Resolve<MemLabelId>("?kMemTypeTree@@3UMemLabelId@@A");
                constructor.Invoke(out Tree, label, allocatePrivateData: false);
                Nodes = CreateNodes(owner);
            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<TypeTree, byte>(ref Tree);
            }

            IReadOnlyList<TypeTreeNode> CreateNodes(ManagedTypeTree owner)
            {
                var nodes = new TypeTreeNode[Tree.Data->Nodes.Size];

                for (int i = 0; i < nodes.Length; i++)
                    nodes[i] = new TypeTreeNode(new TypeTreeNode.V2(Tree.Data->Nodes.Ptr[i]), owner);

                return nodes;
            }

            internal struct TypeTree
            {
                public TypeTreeShareableData* Data;
                public TypeTreeShareableData PrivateData;
            }

            internal struct TypeTreeShareableData
            {
                public DynamicArray<TypeTreeNode.V2.TypeTreeNode> Nodes;
                public DynamicArray<byte> StringBuffer;
                public DynamicArray<uint> ByteOffsets;
                public TransferInstructionFlags FlagsAtGeneration;
                public int RefCount;
                public MemLabelId* MemLabel;
            }
        }
    }
}
