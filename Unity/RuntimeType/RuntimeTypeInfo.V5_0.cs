using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ManagedRuntimeTypeInfo = Unity.RuntimeTypeInfo;

namespace Unity
{
    public partial class RuntimeTypeInfo
    {
        // Unity 5.0 - 5.1
        unsafe class V5_0 : IRuntimeTypeInfoImpl
        {
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            unsafe delegate char* CStrDelegate(TypeTreeString* @this);

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


            public V5_0(IntPtr ptr, SymbolResolver resolver, UnityVersion version)
            {
                var cstr = (delegate* unmanaged[Thiscall]<void*, sbyte*>)resolver.ResolveFirstMatch(
                    new Regex(Regex.Escape("?c_str@?$basic_string@") + "*"));
                TypeInfo = (RuntimeTypeInfo*)ptr;
                var str = cstr(&TypeInfo->ClassName);
                Base = TypeInfo->Base != null ? new ManagedRuntimeTypeInfo(new IntPtr(TypeInfo->Base), resolver, version) : null;
                Name = str != null ? Marshal.PtrToStringAnsi(new IntPtr(str)) : null;
            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<RuntimeTypeInfo, byte>(ref *TypeInfo);
            }

            internal struct TypeTreeString
            {
                public fixed byte Data[40];
            } 

            internal unsafe struct RuntimeTypeInfo
            {
                public RuntimeTypeInfo* Base;
                public IntPtr Factory;
                public PersistentTypeID PersistentTypeID;
                public int Unknown;
                public TypeTreeString ClassName;
                public int Size;
                [MarshalAs(UnmanagedType.U1)]
                public bool IsAbstract;
            }
        }
    }
}
