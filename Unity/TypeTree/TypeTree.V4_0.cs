using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ManagedTypeTree = Unity.TypeTree;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

namespace Unity
{
    public partial class TypeTree
    {
        unsafe class V4_0 : ITypeTreeImpl
        {
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            delegate void TypeTreeDelegate(out TypeTree tree);

            readonly CStrDelegate s_CStr;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            unsafe delegate IntPtr CStrDelegate(ref TypeTreeString self);

            internal TypeTree Tree;

            public IReadOnlyList<byte> StringBuffer => m_StringBuffer;

            public IReadOnlyList<TypeTreeNode> Nodes => m_Nodes;

            public IReadOnlyList<uint> ByteOffsets => m_ByteOffsets;

            private List<uint> m_ByteOffsets;

            private List<TypeTreeNode> m_Nodes;

            private List<byte> m_StringBuffer;

            private Dictionary<string, uint> m_StringBufferIndices;

            private StreamWriter sw;

            public V4_0(ManagedTypeTree owner, SymbolResolver resolver)
            {
                var constructor = resolver.ResolveFunction<TypeTreeDelegate>("??0TypeTree@@QAE@XZ");
                constructor.Invoke(out Tree);

                s_CStr = resolver.ResolveFirstFunctionMatching<CStrDelegate>(
                    new Regex(Regex.Escape("?c_str@?$basic_string@") + "*"));
            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<TypeTree, byte>(ref Tree);
            }

            public void CreateNodes(ManagedTypeTree owner)
            {
                //Debugger.Launch();
                m_StringBuffer = new List<byte>();
                m_StringBufferIndices = new Dictionary<string, uint>();
                m_Nodes = new List<TypeTreeNode>();
                m_ByteOffsets = new List<uint>();
                var type = Marshal.PtrToStringAnsi(s_CStr(ref Tree.m_Type));
                sw = new StreamWriter($"{type}.txt");
                CreateNodes(owner, ref m_Nodes, ref Tree);
                sw.Dispose();
            }

            public void CreateNodes(ManagedTypeTree owner, ref List<TypeTreeNode> nodes, ref TypeTree tree, int level = 0)
            {
                var typeIndex = GetOrCreateStringIndex(tree.m_Type);
                var type = m_StringBufferIndices.First(kv => kv.Value == typeIndex).Key;
                var nameIndex = GetOrCreateStringIndex(tree.m_Name);
                var name = m_StringBufferIndices.First(kv => kv.Value == nameIndex).Key;
                var nodeImpl = new TypeTreeNode.V1(
                    version: (short)tree.m_Version,
                    level: (byte)level,
                    typeFlags: (TypeFlags)tree.m_IsArray,
                    typeStrOffset: typeIndex,
                    nameStrOffset: nameIndex,
                    byteSize: tree.m_ByteSize,
                    index: tree.m_Index,
                    metaFlag: tree.m_MetaFlag);
                nodes.Add(new TypeTreeNode(nodeImpl, owner));
                m_ByteOffsets.Add((uint)tree.m_ByteOffset);
                sw.WriteLine("{0}Type: {1} Name: {2}: Children: {3} Padding1: {4} Padding2: {5}",
                    new string(' ', level),
                    type,
                    name,
                    tree.m_Children.Size,
                    tree.m_Children.Padding1,
                    tree.m_Children.Padding2);
                var node = tree.m_Children.Head;
                for(int i = 0; i < tree.m_Children.Size; i++)
                {
                    node = node->Next;
                    var child = node->Value;
                    CreateNodes(owner, ref nodes, ref child, level + 1);
                }
            }

            uint GetOrCreateStringIndex(TypeTreeString typeTreeString)
            {
                var str = Marshal.PtrToStringAnsi(s_CStr(ref typeTreeString));
                if (m_StringBufferIndices.TryGetValue(str, out var key))
                {
                    return key;
                }
                var newKey = (uint)m_StringBuffer.Count;
                m_StringBufferIndices[str] = newKey;
                foreach (byte b in str)
                {
                    m_StringBuffer.Add(b);
                }
                m_StringBuffer.Add(0);
                return newKey;
            }

            [StructLayout(LayoutKind.Explicit)]
            internal struct TypeTreeString
            {
                [FieldOffset(0)]
                fixed byte Buffer[16];
                [FieldOffset(0)]
                IntPtr Ptr;
                [FieldOffset(16)]
                uint Size;
                [FieldOffset(20)]
                uint Reserved;
                [FieldOffset(24)]
                uint Padding;

                public override string ToString()
                {
                    if (Size < 16)
                    {
                        string result = "";
                        for(int i = 0; i < Size; i++)
                        {
                            result += (char)Buffer[i];
                        }
                        return result;
                    } else
                    {
                        return Marshal.PtrToStringAnsi(Ptr);
                    }
                }
            };

            internal unsafe struct TypeTreeList
            {
                public TypeTreeListNode* Head;
                public uint Size;
                public int Padding1;
                public int Padding2;
            };

            internal unsafe struct TypeTreeListNode
            {
                public TypeTreeListNode* Next;
                public TypeTreeListNode* Prev;
                public TypeTree Value;
            }

            internal struct TypeTree
            {
                public TypeTreeList m_Children;
                public TypeTree* m_Father;
                public TypeTreeString m_Type;
                public TypeTreeString m_Name;
                public int m_ByteSize;
                public int m_Index;
                public int m_IsArray;
                public int m_Version;
                public TransferMetaFlags m_MetaFlag;
                public int m_ByteOffset;
                public void* m_DirectPtr;
            }
        }
    }
}
