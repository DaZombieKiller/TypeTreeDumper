using System;
using Unity;

namespace TypeTreeDumper
{
    public interface IDumperEngine
    {
        event Action<UnityEngine, ExportOptions> OnExportCompleted;
    }
}
