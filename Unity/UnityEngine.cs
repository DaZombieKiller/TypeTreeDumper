namespace Unity
{
    // todo: should all classes just take a UnityEngine argument?
    public class UnityEngine
    {
        public UnityVersion Version { get; }

        public CommonString CommonString { get; }

        public TypeTreeFactory TypeTreeFactory { get; }

        public RuntimeTypeArray RuntimeTypes { get; }

        public NativeObjectFactory ObjectFactory { get; }


        public UnityEngine(UnityVersion version, SymbolResolver resolver)
        {
            Version       = version;
            CommonString  = new CommonString(resolver);
            RuntimeTypes  = new RuntimeTypeArray(version, resolver);
            TypeTreeFactory = new TypeTreeFactory(version, CommonString, resolver);
            ObjectFactory = new NativeObjectFactory(version, resolver);
        }
    }
}
