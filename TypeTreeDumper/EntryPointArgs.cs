using System;

namespace TypeTreeDumper
{
    [Serializable]
    public struct EntryPointArgs
    {
        public string OutputPath;
        public string ProjectPath;
        public bool Verbose;
        public bool Silent;
    }
}
