using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public class RuntimeTypeInfo
    {
        public string Name { get; }

        public string Namespace { get; }

        public string Module { get; }

        internal RuntimeTypeInfo(NativeTypeInfo native)
        {
            Name      = native.ClassName      != IntPtr.Zero ? Marshal.PtrToStringAnsi(native.ClassName)      : null;
            Namespace = native.ClassNamespace != IntPtr.Zero ? Marshal.PtrToStringAnsi(native.ClassNamespace) : null;
            Module    = native.Module         != IntPtr.Zero ? Marshal.PtrToStringAnsi(native.Module)         : null;
        }

        internal unsafe struct NativeTypeInfo
        {
            public NativeTypeInfo* Base;
            public IntPtr Factory;
            public IntPtr ClassName;
            public IntPtr ClassNamespace;
            public IntPtr Module;
            public int PersistentTypeID;
            public int Size;
            public DerivedFromInfo DerivedFromInfo;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsAbstract;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsSealed;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsEditorOnly;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsStripped;
            public IntPtr Attributes;
            public ulong AttributeCount;
        }

        internal struct DerivedFromInfo
        {
            public uint TypeIndex;
            public uint DescendantCount;
        }
    }
}
