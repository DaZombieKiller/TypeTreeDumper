using System;
using System.IO;

namespace TypeTreeDumper
{
    public class IpcInterface : MarshalByRefObject
    {
        public TextReader In { get; }

        public TextWriter Out { get; }

        public TextWriter Error { get; }

        public string OutputDirectory { get; }

        public IpcInterface(TextReader @in, TextWriter @out, TextWriter error, string outputDirectory)
        {
            In              = @in;
            Out             = @out;
            Error           = error;
            OutputDirectory = outputDirectory;
        }

        public void Ping()
        {
        }
    }
}
