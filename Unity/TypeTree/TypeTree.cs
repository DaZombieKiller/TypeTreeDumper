using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Unity
{
    public unsafe partial class TypeTree
    {
        readonly CommonString strings;

        readonly ITypeTreeImpl tree;

        public TypeTree(UnityVersion version, CommonString strings, SymbolResolver resolver)
        {
            if (version < UnityVersion.Unity3_5)
                tree = new V3_4(this, resolver);
            else if (version < UnityVersion.Unity4_0)
                tree = new V3_5(this, resolver);
            else if (version < UnityVersion.Unity5_0)
                tree = new V4_0(this, resolver);
            else if (version < UnityVersion.Unity5_3)
                tree = new V5_0(this, resolver);
            else if (version < UnityVersion.Unity2019_1)
                tree = new V5_3(this, resolver);
            else if (version < UnityVersion.Unity2019_3)
                tree = new V2019_1(this, resolver);
            else
                tree = new V2019_3(this, resolver);

            this.strings = strings;
        }

        public ref byte GetPinnableReference()
        {
            return ref tree.GetPinnableReference();
        }

        public string GetString(uint offset)
        {
            if (offset > int.MaxValue)
                return Marshal.PtrToStringAnsi((IntPtr)(strings.BufferBegin + (int)(int.MaxValue & offset)));

            string str = "";
            for(int i = (int)offset; tree.StringBuffer[i] != 0; i++)
            {
                str += (char)tree.StringBuffer[i];
            }
            return str;
        }

        public void CreateNodes()
        {
            tree.CreateNodes(this);
        }

        public TypeTreeNode this[int index] => tree.Nodes[index];

        public int Count => tree.Nodes.Count;

        public IReadOnlyList<byte> StringBuffer => tree.StringBuffer;

        public IReadOnlyList<uint> ByteOffsets => tree.ByteOffsets;

        interface ITypeTreeImpl
        {
            IReadOnlyList<byte> StringBuffer { get; }
            IReadOnlyList<TypeTreeNode> Nodes { get; }
            IReadOnlyList<uint> ByteOffsets { get; }
            ref byte GetPinnableReference();
            void CreateNodes(TypeTree tree);
        }
    }
}
