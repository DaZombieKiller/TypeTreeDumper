using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Unity
{
    public partial class TypeTree
    {
        unsafe class V1 : ITypeTreeImpl
        {
            public TypeTree Tree;

            public DynamicArray<byte> StringBuffer => Tree.StringBuffer;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void TypeTreeDelegate(out TypeTree tree, MemLabelId* label);

            public V1(SymbolResolver resolver)
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
                public DynamicArray Nodes;
                public DynamicArray<byte> StringBuffer;
                public DynamicArray<uint> ByteOffsets;
            }
        }
    }
}
