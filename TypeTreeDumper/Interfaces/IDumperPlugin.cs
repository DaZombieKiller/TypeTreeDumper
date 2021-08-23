using Unity;

namespace TypeTreeDumper.Interfaces
{
    public interface IDumperPlugin
    {
        void Initialize(IDumperEngine dumper);
        bool TryGetInterface<T>(UnityVersion version, out T result);
    }
}
