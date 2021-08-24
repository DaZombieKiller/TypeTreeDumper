using Unity;

namespace TypeTreeDumper
{
    public interface IDumperPlugin
    {
        void Initialize(IDumperEngine dumper);
        bool TryGetInterface<T>(UnityVersion version, out T result);
    }
}
