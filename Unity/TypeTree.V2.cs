using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Unity
{
    public partial class TypeTree
    {
        // Unity 2019.1 - 2019.2
        unsafe class V2 : ITypeTreeImpl
        {
            public TypeTree Tree;

            public DynamicArray<byte> StringBuffer => Tree.Data->StringBuffer;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void TypeTreeDelegate(out TypeTree tree, MemLabelId* label, bool allocatePrivateData);

            public V2(SymbolResolver resolver)
            {
                var constructor = resolver.ResolveFunction<TypeTreeDelegate>("??0TypeTree@@QEAA@AEBUMemLabelId@@_N@Z");
                var label       = resolver.Resolve<MemLabelId>("?kMemTypeTree@@3UMemLabelId@@A");
                constructor.Invoke(out Tree, label, allocatePrivateData: false);
            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<TypeTree, byte>(ref Tree);
            }

            public struct TypeTree
            {
                public TypeTreeShareableData* Data;
                public TypeTreeShareableData PrivateData;
            }

            public struct TypeTreeShareableData
            {
                public DynamicArray Nodes;
                public DynamicArray<byte> StringBuffer;
                public DynamicArray<uint> ByteOffsets;
                public TransferInstructionFlags FlagsAtGeneration;
                public int RefCount;
                public MemLabelId* MemLabel;
            }
        }
    }
}
