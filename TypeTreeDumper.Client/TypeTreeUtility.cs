using System;
using System.IO;
using Unity;

namespace TypeTreeDumper
{
    internal class TypeTreeUtility
    {
        internal static void CreateBinaryDump(TypeTree tree, BinaryWriter writer)
        {
            writer.Write(tree.Count);
            writer.Write(tree.StringBuffer.Length);
            for (int i = 0, n = tree.Count; i < n; i++)
            {
                var node = tree[i];
                writer.Write(node.Version);
                writer.Write(node.Level);
                writer.Write((byte)node.TypeFlags);
                writer.Write(node.TypeStrOffset);
                writer.Write(node.NameStrOffset);
                writer.Write(node.ByteSize);
                writer.Write(node.Index);
                writer.Write((int)node.MetaFlag);
            }
            for (int i = 0, n = tree.StringBuffer.Length; i < n; i++)
                writer.Write(tree.StringBuffer[i]);
        }

        internal static void CreateTextDump(TypeTree tree, StreamWriter writer)
        {
            for (int i = 0; i < tree.Count; i++)
            {
                var node = tree[i];
                for (int j = 0; j < node.Level; j++)
                    writer.Write("  ");
                string type = node.TypeName;
                string name = node.Name;
                writer.WriteLine(string.Format("{0} {1} // ByteSize{{{2}}}, Index{{{3}}}, IsArray{{{4}}}, MetaFlag{{{5}}}",
                    type,
                    name,
                    node.ByteSize.ToString("x"),
                    node.Index.ToString("x"),
                    (byte)node.TypeFlags,
                    ((int)node.MetaFlag).ToString("x")
                ));
            }
        }
    }
}
