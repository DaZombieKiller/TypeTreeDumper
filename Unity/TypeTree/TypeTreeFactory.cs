using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public unsafe class TypeTreeFactory
    {
        readonly CommonString strings;

        readonly UnityVersion version;

        readonly SymbolResolver resolver;

        readonly delegate* unmanaged[Cdecl]<void*, TransferInstructionFlags, void*, byte> getTypeTree;

        readonly delegate* unmanaged[Cdecl]<void*, void*, TransferInstructionFlags, void> generateTypeTree;

        bool HasGetTypeTree => version.Major >= 2019;

        public TypeTreeFactory(UnityVersion version, CommonString strings, SymbolResolver resolver)
        {
            this.version  = version;
            this.resolver = resolver;
            this.strings  = strings;

            if (HasGetTypeTree)
                getTypeTree = (delegate* unmanaged[Cdecl]<void*, TransferInstructionFlags, void*, byte>)resolver.Resolve($"?GetTypeTree@TypeTreeCache@@YA_NP{NameMangling.Ptr64}BVObject@@W4TransferInstructionFlags@@A{NameMangling.Ptr64}AVTypeTree@@@Z");
            else
            {
                generateTypeTree = (delegate* unmanaged[Cdecl]<void*, void*, TransferInstructionFlags, void>)resolver.Resolve(
                    $"?GenerateTypeTree@@YAXA{NameMangling.Ptr64}BVObject@@A{NameMangling.Ptr64}AVTypeTree@@W4TransferInstructionFlags@@@Z",
                    $"?GenerateTypeTree@@YAXA{NameMangling.Ptr64}AVObject@@P{NameMangling.Ptr64}AVTypeTree@@W4TransferInstructionFlags@@@Z",
                    $"?GenerateTypeTree@@YAXA{NameMangling.Ptr64}AVObject@@P{NameMangling.Ptr64}AVTypeTree@@H@Z"
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
                    if (getTypeTree(@object.Pointer, flags, pointer) == 0)
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
