using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ManagedRuntimeTypeInfo = Unity.RuntimeTypeInfo;

namespace Unity
{
    public partial class RuntimeTypeInfo
    {
        // Unity 5.2 - 5.3
        unsafe class V5_2 : IRuntimeTypeInfoImpl
        {
            RuntimeTypeInfo* TypeInfo;

            public ManagedRuntimeTypeInfo Base { get; }

            public string Name { get; }

            public string Namespace => null;

            public string Module => null;

            public PersistentTypeID PersistentTypeID => TypeInfo->PersistentTypeID;

            public int Size => TypeInfo->Size;

            public uint TypeIndex => 0;

            public uint DescendantCount => 0;

            public bool IsAbstract => TypeInfo->IsAbstract;

            public bool IsSealed => false;

            public bool IsEditorOnly => false;

            public bool IsStripped => false;

            public IntPtr Attributes => IntPtr.Zero;

            public ulong AttributeCount => 0;


            public V5_2(IntPtr ptr, SymbolResolver resolver, UnityVersion version)
            {
                TypeInfo = (RuntimeTypeInfo*)ptr;
                Base = TypeInfo->Base != null ? new ManagedRuntimeTypeInfo(new IntPtr(TypeInfo->Base), resolver, version) : null;
                Name = TypeInfo->ClassName != IntPtr.Zero ? Marshal.PtrToStringAnsi(TypeInfo->ClassName) : null;            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<RuntimeTypeInfo, byte>(ref *TypeInfo);
            }

            internal unsafe struct RuntimeTypeInfo
            {
                public RuntimeTypeInfo* Base;
                public IntPtr Factory;
                public PersistentTypeID PersistentTypeID;
                public IntPtr ClassName;
                public int Size;
                [MarshalAs(UnmanagedType.U1)]
                public bool IsAbstract;
            }
        }
    }
}
