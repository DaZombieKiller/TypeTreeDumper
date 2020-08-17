using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public unsafe partial class TypeTree
    {
        readonly CommonString strings;

        readonly ITypeTreeImpl tree;

        public TypeTree(UnityVersion version, CommonString strings, SymbolResolver resolver)
        {
            if (version >= UnityVersion.Unity2019_1 && version < UnityVersion.Unity2019_3)
                tree = new V2(resolver);
            else if (version >= UnityVersion.Unity2019_3)
                tree = new V3(resolver);
            else
                tree = new V1(resolver);

            this.strings = strings;
            // todo: store size of TypeTreeNode for indexing nodes array
        }

        public ref byte GetPinnableReference()
        {
            return ref tree.GetPinnableReference();
        }

        public string GetString(uint offset)
        {
            if (offset > int.MaxValue)
                return Marshal.PtrToStringAnsi(IntPtr.Add(strings.BufferBegin, (int)(0x7fffffff & offset)));

            return Marshal.PtrToStringAnsi(new IntPtr(tree.StringBuffer.Ptr + offset));
        }

        interface ITypeTreeImpl
        {
            DynamicArray<byte> StringBuffer { get; }
            ref byte GetPinnableReference();
        }
    }
}
