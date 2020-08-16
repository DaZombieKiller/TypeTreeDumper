namespace Unity
{
    public class UnityEngine
    {
        public UnityVersion Version { get; }

        public CommonString CommonString { get; }

        public TypeTreeCache TypeTreeCache { get; }

        public RuntimeTypeArray RuntimeTypes { get; }

        public UnityEngine(UnityVersion version, SymbolResolver resolver)
        {
            Version       = version;
            CommonString  = new CommonString(resolver);
            TypeTreeCache = new TypeTreeCache(version, resolver);
            RuntimeTypes  = new RuntimeTypeArray(resolver);
        }
    }
}
