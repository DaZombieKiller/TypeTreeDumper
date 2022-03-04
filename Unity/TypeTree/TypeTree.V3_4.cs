using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ManagedTypeTree = Unity.TypeTree;
using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace Unity
{
    public partial class TypeTree
    {
        unsafe class V3_4 : ITypeTreeImpl
        {
            readonly delegate* unmanaged[Thiscall]<TypeTreeString*, sbyte*> s_CStr;

            internal TypeTree Tree;

            public IReadOnlyList<byte> StringBuffer => m_StringBuffer;

            public IReadOnlyList<TypeTreeNode> Nodes => m_Nodes;

            public IReadOnlyList<uint> ByteOffsets => m_ByteOffsets;

            private List<uint> m_ByteOffsets;

            private List<TypeTreeNode> m_Nodes;

            private List<byte> m_StringBuffer;

            private Dictionary<string, uint> m_StringBufferIndices;


            public V3_4(ManagedTypeTree owner, SymbolResolver resolver)
            {
                TypeTree tree;
                var constructor = (delegate* unmanaged[Thiscall]<TypeTree*, void>)resolver.Resolve($"??0TypeTree@@QAE@XZ");
                constructor(&tree);
                Tree = tree;

                s_CStr = (delegate* unmanaged[Thiscall]<TypeTreeString*, sbyte*>)resolver.ResolveFirstMatch(
                    new Regex(Regex.Escape("?c_str@?$basic_string@") + "*"));
            }

            public ref byte GetPinnableReference()
            {
                return ref Unsafe.As<TypeTree, byte>(ref Tree);
            }

            public void CreateNodes(ManagedTypeTree owner)
            {
                m_StringBuffer = new List<byte>();
                m_StringBufferIndices = new Dictionary<string, uint>();
                m_Nodes = new List<TypeTreeNode>();
                m_ByteOffsets = new List<uint>();
                CreateNodes(owner, ref m_Nodes, ref Tree);
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
                var tts = typeTreeString;
                var str = Marshal.PtrToStringAnsi((IntPtr)s_CStr(&typeTreeString));
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
                public uint Unknown;
                [FieldOffset(4)]
                fixed byte Buffer[16];
                [FieldOffset(4)]
                IntPtr Ptr;
                [FieldOffset(20)]
                public uint Size;
                [FieldOffset(24)]
                public uint Reserved;

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
                public uint Unknown1;
                public TypeTreeListNode* Head;
                public int Size;
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
