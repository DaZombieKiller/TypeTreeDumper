using Newtonsoft.Json;
using System.Collections.Generic;
using Unity;

namespace TypeTreeDumper
{
    internal class UnityNode
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
        public byte Level { get; set; }
        public int ByteSize { get; set; }
        public int Index { get; set; }
        public short Version { get; set; }
        public byte TypeFlags { get; set; }
        public uint MetaFlag { get; set; }
        public List<UnityNode> SubNodes { get; set; }
        [JsonIgnore]
        public UnityNode Parent { get; set; }

        public UnityNode() { }
        public UnityNode(UnityNode parent, TypeTreeNode treeNode) : this(treeNode)
        {
            Parent = parent;
        }
        public UnityNode(TypeTreeNode treeNode)
        {
            Level = treeNode.Level;
            TypeName = treeNode.TypeName;
            Name = treeNode.Name;
            ByteSize = treeNode.ByteSize;
            Index = treeNode.Index;
            Version = treeNode.Version;
            TypeFlags = (byte)treeNode.TypeFlags;
            MetaFlag = (uint)treeNode.MetaFlag;
            SubNodes = new List<UnityNode>();
        }
    }
}
