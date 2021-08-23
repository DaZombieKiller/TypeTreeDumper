using System;
using TypeTreeDumper.Interfaces;
using Unity;

namespace TypeTreeDumper
{
    internal class DumperEngine : IDumperEngine
    {
        public event Action<UnityEngine, ExportOptions> OnExportCompleted;

        internal void InvokeExportCompleted(UnityEngine engine, ExportOptions options) => OnExportCompleted?.Invoke(engine, options);
    }
}
