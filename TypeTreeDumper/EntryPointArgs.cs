using TerraFX.Interop.Windows;

namespace TypeTreeDumper
{
    public struct EntryPointArgs
    {
        public string OutputPath { get; set; }
        public string ProjectPath { get; set; }
        public bool Verbose { get; set; }
        public bool Silent { get; set; }
        public ulong ThreadHandle { get; set; }
    }
}
