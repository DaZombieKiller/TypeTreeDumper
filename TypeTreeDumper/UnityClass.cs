using System;
using System.Collections.Generic;
using System.Linq;
using Unity;

namespace TypeTreeDumper
{
    internal class UnityClass
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string FullName { get; set; }
        public string Module { get; set; }
        public int TypeID { get; set; }
        public string Base { get; set; }
        public List<string> Derived { get; set; }
        public uint DescendantCount { get; set; }
        public int Size { get; set; }
        public uint TypeIndex { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsSealed { get; set; }
        public bool IsEditorOnly { get; set; }
        public bool IsStripped { get; set; }
        public UnityNode EditorRootNode { get; set; }
        public UnityNode ReleaseRootNode { get; set; }

        public UnityClass() { }
        public UnityClass(RuntimeTypeInfo runtimeType)
        {
            Name = runtimeType.Name;
            Namespace = runtimeType.Namespace;
            FullName = runtimeType.FullName;
            Module = runtimeType.Module;
            TypeID = (int)runtimeType.PersistentTypeID;
            Base = runtimeType.Base?.Name ?? "";
            Derived = runtimeType.Derived.ConvertAll(d => d?.Name ?? "");
            DescendantCount = runtimeType.DescendantCount;
            Size = runtimeType.Size;
            TypeIndex = runtimeType.TypeIndex;
            IsAbstract = runtimeType.IsAbstract;
            IsSealed = runtimeType.IsSealed;
            IsEditorOnly = runtimeType.IsEditorOnly;
            IsStripped = runtimeType.IsStripped;
        }

        public static List<UnityClass> MakeList(UnityEngine engine)
        {
            var result = new List<UnityClass>();

            foreach (var type in engine.RuntimeTypes.ToArray().OrderBy(x => (int)x.PersistentTypeID))
            {
                var next = new UnityClass(type);

                var iter = type;
                while (iter.IsAbstract)
                {
                     if (iter.Base == null)
                        break;
                     else
                        iter = iter.Base;
                }

                using var obj = engine.ObjectFactory.GetOrProduce(iter);

                if (obj != null)
                {
                    TypeTree editorTree = engine.TypeTreeFactory.GetTypeTree(obj, TransferInstructionFlags.None);
                    TypeTree releaseTree = engine.TypeTreeFactory.GetTypeTree(obj, TransferInstructionFlags.SerializeGameRelease);

                    next.EditorRootNode = CreateRootNode(editorTree);
                    next.ReleaseRootNode = CreateRootNode(releaseTree);
                }

                result.Add(next);
            }

            return result;
        }

        /// <summary>
        /// Converts a Type Tree into a Node Tree
        /// </summary>
        /// <param name="tree"></param>
        /// <returns>The root of the node tree</returns>
        private static UnityNode CreateRootNode(TypeTree tree)
        {
            if (tree == null)
                throw new ArgumentNullException(nameof(tree));
            UnityNode root = new UnityNode(tree[0]);
            UnityNode current = root;
            for (int i = 1; i < tree.Count; i++)
            {
                TypeTreeNode treeNode = tree[i];

                while (treeNode.Level <= current.Level)
                    current = current.Parent;

                UnityNode newNode = new UnityNode(current, treeNode);
                current.SubNodes.Add(newNode);
                current = newNode;
            }
            return root;
        }
    }
}
