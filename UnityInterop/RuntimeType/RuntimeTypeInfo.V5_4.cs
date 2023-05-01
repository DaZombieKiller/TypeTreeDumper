using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ManagedRuntimeTypeInfo = Unity.RuntimeTypeInfo;

namespace Unity
{
    public partial class RuntimeTypeInfo
    {
        // Unity 5.4
        unsafe class V5_4 : IRuntimeTypeInfoImpl
        {
            RuntimeTypeInfo* TypeInfo;

            public ManagedRuntimeTypeInfo Base { get; }

            public string Name { get; }

            public string Namespace => null;

            public string Module => null;

            public PersistentTypeID PersistentTypeID => TypeInfo->PersistentTypeID;

            public int Size => TypeInfo->Size;

            public uint TypeIndex => TypeInfo->DerivedFromInfo.TypeIndex;

            public uint DescendantCount => TypeInfo->DerivedFromInfo.DescendantCount;

            public bool IsAbstract => TypeInfo->IsAbstract;

            public bool IsSealed => TypeInfo->IsSealed;

            public bool IsEditorOnly => TypeInfo->IsEditorOnly;

            public bool IsStripped => false;

            public IntPtr Attributes => IntPtr.Zero;

            public ulong AttributeCount => 0;


            public V5_4(IntPtr ptr, SymbolResolver resolver, UnityVersion version)
            {
                TypeInfo = (RuntimeTypeInfo*)ptr;
                Base = TypeInfo->Base != null ? new ManagedRuntimeTypeInfo(new IntPtr(TypeInfo->Base), resolver, version) : null;
                Name = TypeInfo->ClassName != IntPtr.Zero ? Marshal.PtrToStringAnsi(TypeInfo->ClassName) : null;
            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<RuntimeTypeInfo, byte>(ref *TypeInfo);
            }

            internal unsafe struct RuntimeTypeInfo
            {
                public RuntimeTypeInfo* Base;
                public IntPtr Factory;
                public IntPtr ClassName;
                public PersistentTypeID PersistentTypeID;
                public int Size;
                public DerivedFromInfo DerivedFromInfo;
                [MarshalAs(UnmanagedType.U1)]
                public bool IsAbstract;
                [MarshalAs(UnmanagedType.U1)]
                public bool IsSealed;
                [MarshalAs(UnmanagedType.U1)]
                public bool IsEditorOnly;
            }

            internal struct DerivedFromInfo
            {
                public uint TypeIndex;
                public uint DescendantCount;
            }
        }
    }
}
