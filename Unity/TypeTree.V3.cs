using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Unity
{
    public partial class TypeTree
    {
        // Unity 2019.3+
        unsafe class V3 : ITypeTreeImpl
        {
            public TypeTree Tree;

            public DynamicArray<byte> StringBuffer => Tree.Data->StringBuffer;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void TypeTreeDelegate(out TypeTree tree, MemLabelId* label);

            public V3(SymbolResolver resolver)
            {
                var constructor = resolver.ResolveFunction<TypeTreeDelegate>("??0TypeTree@@QEAA@AEBUMemLabelId@@@Z");
                var label       = resolver.Resolve<MemLabelId>("?kMemTypeTree@@3UMemLabelId@@A");
                constructor.Invoke(out Tree, label);
            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<TypeTree, byte>(ref Tree);
            }

            public struct TypeTree
            {
                public TypeTreeShareableData* Data;
                public IntPtr ReferencedTypes;
                [MarshalAs(UnmanagedType.U1)]
                public bool PoolOwned;
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
