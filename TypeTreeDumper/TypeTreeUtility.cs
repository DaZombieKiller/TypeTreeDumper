using System.IO;
using Unity;

namespace TypeTreeDumper
{
    internal class TypeTreeUtility
    {
        internal static void CreateBinaryDump(TypeTree tree, BinaryWriter writer)
        {
            writer.Write(tree.Count);
            writer.Write(tree.StringBuffer.Count);
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
                writer.Write((uint)node.MetaFlag);
            }
            for (int i = 0, n = tree.StringBuffer.Count; i < n; i++)
                writer.Write(tree.StringBuffer[i]);
        }

        internal static void CreateTextDump(UnityNode node, StreamWriter writer)
        {
            for (int j = 0; j < node.Level; j++)
                writer.Write('\t');
            string type = node.TypeName;
            string name = node.Name;
            writer.WriteLine(string.Format("{0} {1} // ByteSize{{{2}}}, Index{{{3}}}, IsArray{{{4}}}, MetaFlag{{{5}}}",
                type,
                name,
                node.ByteSize.ToString("x"),
                node.Index.ToString("x"),
                (byte)node.TypeFlags,
                ((uint)node.MetaFlag).ToString("x")
            ));

            if (node.SubNodes != null)
            {
                for (int i = 0; i < node.SubNodes.Count; i++)
                {
                    CreateTextDump(node.SubNodes[i], writer);
                }
            }
        }
    }
}
