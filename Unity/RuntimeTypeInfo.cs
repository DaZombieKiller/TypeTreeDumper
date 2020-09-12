using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unity
{
    public unsafe class RuntimeTypeInfo
    {
        NativeTypeInfoUnion native;

        public ref byte GetPinnableReference()
        {
            return ref Unsafe.As<NativeTypeInfoUnion, byte>(ref native);
        }

        public string Name { get; }

        public string Namespace { get; }

        public string Module { get; }

        public PersistentTypeID PersistentTypeID { get; }

        public RuntimeTypeInfo Base { get; }

        public int Size { get; }

        public uint TypeIndex { get;  }

        public uint DescendantCount { get; }

        public bool IsAbstract { get; }

        public bool IsSealed { get; }

        public bool IsEditorOnly { get; }

        public bool IsStripped { get; }

        public IntPtr Attributes { get; }

        public ulong AttributeCount { get; }


        internal RuntimeTypeInfo(NativeTypeInfoV1 native)
        {
            this.native = new NativeTypeInfoUnion() { V1 = native };
            Base = native.Base != null ? new RuntimeTypeInfo(*native.Base) : null;
            Name = native.ClassName != IntPtr.Zero ? Marshal.PtrToStringAnsi(native.ClassName) : null;
            Namespace = native.ClassNamespace != IntPtr.Zero ? Marshal.PtrToStringAnsi(native.ClassNamespace) : null;
            PersistentTypeID = native.PersistentTypeID;
            Size = native.Size;
            TypeIndex = native.DerivedFromInfo.TypeIndex;
            DescendantCount = native.DerivedFromInfo.DescendantCount;
            IsAbstract = native.IsAbstract;
            IsSealed = native.IsSealed;
            IsEditorOnly = native.IsEditorOnly;
        }

        internal RuntimeTypeInfo(NativeTypeInfoV2 native)
        {
            this.native = new NativeTypeInfoUnion() { V2 = native };
            Base             = native.Base           != null        ? new RuntimeTypeInfo(*native.Base)              : null;
            Name             = native.ClassName      != IntPtr.Zero ? Marshal.PtrToStringAnsi(native.ClassName)      : null;
            Namespace        = native.ClassNamespace != IntPtr.Zero ? Marshal.PtrToStringAnsi(native.ClassNamespace) : null;
            Module           = native.Module         != IntPtr.Zero ? Marshal.PtrToStringAnsi(native.Module)         : null;
            PersistentTypeID = native.PersistentTypeID;
            Size = native.Size;
            TypeIndex = native.DerivedFromInfo.TypeIndex;
            DescendantCount = native.DerivedFromInfo.DescendantCount;
            IsAbstract       = native.IsAbstract;
            IsSealed = native.IsSealed;
            IsEditorOnly = native.IsEditorOnly;
            IsStripped = native.IsStripped;
            Attributes = native.Attributes;
            AttributeCount = native.AttributeCount;
        }
        [StructLayout(LayoutKind.Explicit)]
        internal struct NativeTypeInfoUnion
        {
            [FieldOffset(0)]
            public NativeTypeInfoV1 V1;

            [FieldOffset(0)]
            public NativeTypeInfoV2 V2;
        }
        internal unsafe struct NativeTypeInfoV1
        {
            public NativeTypeInfoV1* Base;
            public IntPtr Factory;
            public IntPtr ClassName;
            public IntPtr ClassNamespace;
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

        internal unsafe struct NativeTypeInfoV2
        {
            public NativeTypeInfoV2* Base;
            public IntPtr Factory;
            public IntPtr ClassName;
            public IntPtr ClassNamespace;
            public IntPtr Module;
            public PersistentTypeID PersistentTypeID;
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
