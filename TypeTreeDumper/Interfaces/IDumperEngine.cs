using System;
using Unity;

namespace TypeTreeDumper.Interfaces
{
    public interface IDumperEngine
    {
        event Action<UnityEngine, ExportOptions> OnExportCompleted;
    }
}
