using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public unsafe class TypeTreeFactory
    {
        readonly CommonString strings;

        readonly UnityVersion version;

        readonly SymbolResolver resolver;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate bool GetTypeTreeDelegate(IntPtr @object, TransferInstructionFlags flags, void* tree);

        readonly GetTypeTreeDelegate getTypeTree;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void GenerateTypeTreeDelegate(IntPtr @object, void* tree, TransferInstructionFlags flags);

        readonly GenerateTypeTreeDelegate generateTypeTree;

        bool HasGetTypeTree => version.Major >= 2019;

        public TypeTreeFactory(UnityVersion version, CommonString strings, SymbolResolver resolver)
        {
            this.version = version;
            this.resolver = resolver;
            this.strings = strings;
            if (HasGetTypeTree)
            {
                getTypeTree = resolver.ResolveFunction<GetTypeTreeDelegate>("?GetTypeTree@TypeTreeCache@@YA_NPEBVObject@@W4TransferInstructionFlags@@AEAVTypeTree@@@Z");
            }
            else
            {
                generateTypeTree = resolver.ResolveFunction<GenerateTypeTreeDelegate>(
                    "?GenerateTypeTree@@YAXAEBVObject@@AEAVTypeTree@@W4TransferInstructionFlags@@@Z",
                    "?GenerateTypeTree@@YAXAEAVObject@@PEAVTypeTree@@W4TransferInstructionFlags@@@Z",
                    "?GenerateTypeTree@@YAXAAVObject@@PAVTypeTree@@H@Z"
                );
            }
        }

        public unsafe TypeTree GetTypeTree(NativeObject @object, TransferInstructionFlags flags)
        {
            var tree = new TypeTree(version, strings, resolver);

            fixed (byte* pointer = tree)
            {
                if (HasGetTypeTree)
                {
                    if (!getTypeTree(@object.Pointer, flags, pointer))
                    {
                        throw new InvalidOperationException("Failed to get type tree");
                    }
                }
                else
                {
                    generateTypeTree(@object.Pointer, pointer, flags);
                }
            }

            tree.CreateNodes();

            return tree;
        }
    }
}
