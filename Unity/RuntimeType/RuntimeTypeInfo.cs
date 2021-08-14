using System;

namespace Unity
{
    public partial class RuntimeTypeInfo
    {
        IRuntimeTypeInfoImpl TypeInfo;

        public ref byte GetPinnableReference()
        {
            return ref TypeInfo.GetPinnableReference();
        }

        public string Name => TypeInfo.Name;

        public string Namespace => TypeInfo.Namespace;

        public string Module => TypeInfo.Module;

        public PersistentTypeID PersistentTypeID => TypeInfo.PersistentTypeID;

        public RuntimeTypeInfo Base => TypeInfo.Base;

        public int Size => TypeInfo.Size;

        public uint TypeIndex => TypeInfo.TypeIndex;

        public uint DescendantCount => TypeInfo.DescendantCount;

        public bool IsAbstract => TypeInfo.IsAbstract;

        public bool IsSealed => TypeInfo.IsSealed;

        public bool IsEditorOnly => TypeInfo.IsEditorOnly;

        public bool IsStripped => TypeInfo.IsStripped;

        public IntPtr Attributes => TypeInfo.Attributes;

        public ulong AttributeCount => TypeInfo.AttributeCount;

        internal RuntimeTypeInfo(IntPtr ptr, SymbolResolver resolver, UnityVersion version)
        {
            if (version >= UnityVersion.Unity2017_3)
            {
                TypeInfo = new V2017_3(ptr, resolver, version);
            }
            else if (version >= UnityVersion.Unity5_5)
            {
                TypeInfo = new V5_5(ptr, resolver, version);
            }
            else if (version >= UnityVersion.Unity5_4)
            {
                TypeInfo = new V5_4(ptr, resolver, version);
            }
            else if (version >= UnityVersion.Unity5_2)
            {
                TypeInfo = new V5_2(ptr, resolver, version);
            }
            else if (version >= UnityVersion.Unity5_0)
            {
                TypeInfo = new V5_0(ptr, resolver, version);
            }
            else
            {
                TypeInfo = new V3_4(ptr, resolver, version);
            }
        }

        interface IRuntimeTypeInfoImpl
        {
            RuntimeTypeInfo Base { get;  }
            public string Name { get; }
            public string Namespace { get; }
            public string Module { get; }
            public PersistentTypeID PersistentTypeID { get; }
            public int Size { get; }
            public uint TypeIndex { get; }
            public uint DescendantCount { get; }
            public bool IsAbstract { get; }
            public bool IsSealed { get; }
            public bool IsEditorOnly { get; }
            public bool IsStripped { get; }
            public IntPtr Attributes { get; }
            public ulong AttributeCount { get; }
            ref byte GetPinnableReference();
        }
    }
}
