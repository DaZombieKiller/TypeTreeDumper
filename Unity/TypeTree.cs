using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public unsafe class TypeTree : IDisposable
    {
        TypeTreeUnion* union;

        UnityVersion version;

        bool allocated;

        CommonString strings;

        public IntPtr Pointer => new IntPtr(union);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void TypeTreeDelegate(TypeTreeUnion* tree, MemLabelId* label);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void TypeTreeV2Delegate(TypeTreeUnion* tree, MemLabelId* label, bool allocatePrivateData);

        public TypeTree(UnityVersion version, CommonString strings, IntPtr address)
        {
            union        = (TypeTreeUnion*)address;
            this.version = version;
            this.strings = strings;
        }

        public TypeTree(UnityVersion version, SymbolResolver resolver)
        {
            allocated    = true;
            union        = (TypeTreeUnion*)Marshal.AllocHGlobal(sizeof(TypeTreeUnion));
            var label    = resolver.Resolve<MemLabelId>("?kMemTypeTree@@3UMemLabelId@@A");
            this.version = version;

            if (TypeTreeVersion == 2)
            {
                var constructor = resolver.ResolveFunction<TypeTreeV2Delegate>("??0TypeTree@@QEAA@AEBUMemLabelId@@_N@Z");
                constructor.Invoke(union, label, false);
            }
            else
            {
                var constructor = resolver.ResolveFunction<TypeTreeDelegate>("??0TypeTree@@QEAA@AEBUMemLabelId@@@Z");
                constructor.Invoke(union, label);
            }
        }

        public void Dispose()
        {
            if (allocated && union != null)
            {
                Marshal.FreeHGlobal(new IntPtr(union));
                union = null;
            }
        }

        internal string GetString(uint offset)
        {
            if (offset > int.MaxValue)
                return Marshal.PtrToStringAnsi(IntPtr.Add(strings.BufferBegin, (int)(0x7fffffff & offset)));

            return Marshal.PtrToStringAnsi(new IntPtr(StringBuffer.Ptr + offset));
        }

        DynamicArray<byte> StringBuffer => TypeTreeVersion switch
        {
            1 => union->V1.StringBuffer,
            2 => union->V2.Data->StringBuffer,
            3 => union->V3.Data->StringBuffer,
            _ => throw null
        };

        int TypeTreeVersion
        {
            get
            {
                if (version >= UnityVersion.Unity2019_1 && version < UnityVersion.Unity2019_3)
                    return 2;
                else if (version >= UnityVersion.Unity2019_3)
                    return 3;
                else
                    return 1;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        struct TypeTreeUnion
        {
            [FieldOffset(0)]
            public TypeTreeV1 V1;

            [FieldOffset(0)]
            public TypeTreeV2 V2;

            [FieldOffset(0)]
            public TypeTreeV3 V3;
        }

        struct TypeTreeV1
        {
            public DynamicArray Nodes;
            public DynamicArray<byte> StringBuffer;
            public DynamicArray<uint> ByteOffsets;
        }

        // Unity 2019.1 - 2019.2
        unsafe struct TypeTreeV2
        {
            public TypeTreeShareableData* Data;
            public TypeTreeShareableData PrivateData;
        }

        // Unity 2019.3+
        unsafe struct TypeTreeV3
        {
            public TypeTreeShareableData* Data;
            public IntPtr ReferencedTypes;
            [MarshalAs(UnmanagedType.U1)]
            public bool PoolOwned;
        }

        unsafe struct TypeTreeShareableData
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
