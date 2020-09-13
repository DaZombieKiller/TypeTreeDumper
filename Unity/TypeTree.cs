﻿using System;
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
            if (version < UnityVersion.Unity2019_1)
                tree = new V1(this, resolver);
            else if (version < UnityVersion.Unity2019_3)
                tree = new V2(this, resolver);
            else
                tree = new V3(this, resolver);

            this.strings = strings;
        }

        public ref byte GetPinnableReference()
        {
            return ref tree.GetPinnableReference();
        }

        public string GetString(uint offset)
        {
            if (offset > int.MaxValue)
                return Marshal.PtrToStringAnsi(IntPtr.Add(strings.BufferBegin, (int)(int.MaxValue & offset)));

            return Marshal.PtrToStringAnsi(new IntPtr(tree.StringBuffer.Ptr + offset));
        }

        public void CreateNodes()
        {
            tree.CreateNodes(this);
            StringBuffer = new byte[tree.StringBuffer.Size];
            fixed (byte* destination = StringBuffer)
                Buffer.MemoryCopy(tree.StringBuffer.Ptr, destination, StringBuffer.Length, StringBuffer.Length);
        }

        public TypeTreeNode this[int index] => tree.Nodes[index];

        public int Count => tree.Nodes.Count;

        public byte[] StringBuffer;

        interface ITypeTreeImpl
        {
            DynamicArray<byte> StringBuffer { get; }
            IReadOnlyList<TypeTreeNode> Nodes { get; }
            ref byte GetPinnableReference();
            void CreateNodes(TypeTree tree);
        }
    }
}