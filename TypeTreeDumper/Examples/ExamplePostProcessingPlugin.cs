using Unity;

namespace TypeTreeDumper.Examples
{
    public class ExamplePostProcessingPlugin : IDumperPlugin
    {
        public void Initialize(IDumperEngine dumper)
        {
            dumper.OnExportCompleted += PostProcessExport;
        }

        public bool TryGetInterface<T>(UnityVersion version, out T result)
        {
            // This plugin doesn't provide any engine interfaces
            result = default;
            return false;
        }

        void PostProcessExport(UnityEngine engine, ExportOptions options)
        {
        }
    }
}
