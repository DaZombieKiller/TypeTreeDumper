using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public class TypeTreeCache
    {
        readonly CommonString strings;
        
        readonly UnityVersion version;

        readonly SymbolResolver resolver;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate bool GetTypeTreeDelegate(IntPtr @object, TransferInstructionFlags flags, void* tree);

        readonly GetTypeTreeDelegate getTypeTree;

        public TypeTreeCache(UnityVersion version, CommonString strings, SymbolResolver resolver)
        {
            this.version  = version;
            this.resolver = resolver;
            this.strings  = strings;
            getTypeTree   = resolver.ResolveFunction<GetTypeTreeDelegate>("?GetTypeTree@TypeTreeCache@@YA_NPEBVObject@@W4TransferInstructionFlags@@AEAVTypeTree@@@Z");
        }

        public unsafe TypeTree GetTypeTree(IntPtr @object, TransferInstructionFlags flags)
        {
            if (getTypeTree == null)
                throw new NotSupportedException();

            var tree = new TypeTree(version, strings, resolver);

            fixed (byte* pointer = tree)
                getTypeTree.Invoke(@object, flags, pointer);

            return tree;
        }
    }
}
