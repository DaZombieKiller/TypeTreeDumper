using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public class TypeTreeCache
    {
        UnityVersion version;
        SymbolResolver resolver;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool GetTypeTreeDelegate(IntPtr @object, TransferInstructionFlags flags, IntPtr tree);
        readonly GetTypeTreeDelegate getTypeTree;

        public TypeTreeCache(UnityVersion version, SymbolResolver resolver)
        {
            this.version  = version;
            this.resolver = resolver;
            getTypeTree   = resolver.ResolveFunction<GetTypeTreeDelegate>("?GetTypeTree@TypeTreeCache@@YA_NPEBVObject@@W4TransferInstructionFlags@@AEAVTypeTree@@@Z");
        }

        public TypeTree GetTypeTree(IntPtr @object, TransferInstructionFlags flags)
        {
            if (getTypeTree == null)
                throw new NotSupportedException();

            var tree = new TypeTree(version, resolver);
            getTypeTree(@object, flags, tree.Pointer);
            return tree;
        }
    }
}
