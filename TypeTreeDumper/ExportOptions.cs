using Unity;

namespace TypeTreeDumper
{
    public class ExportOptions
    {
        public string OutputDirectory { get; set; }

        public TransferInstructionFlags TransferFlags { get; set; } = TransferInstructionFlags.SerializeGameRelease;

        public bool ExportTextDump { get; set; } = true;

        public bool ExportBinaryDump { get; set; } = true;

        public bool ExportClassesJson { get; set; } = true;

        public ExportOptions() { }

        public ExportOptions(string outputDirectory) => OutputDirectory = outputDirectory;
    }
}
