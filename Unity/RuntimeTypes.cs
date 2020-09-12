using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Unity
{
    public class RuntimeTypeArray : IReadOnlyList<RuntimeTypeInfo>
    {
        readonly List<RuntimeTypeInfo> types;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate NativeTypeArray* GetRuntimeTypesDelegate();

        public RuntimeTypeInfo this[int index] => types[index];

        public int Count => types.Count;

        unsafe public RuntimeTypeArray(UnityVersion version, SymbolResolver resolver)
        {

            NativeTypeArray* runtimeTypes;
            if (version >= UnityVersion.Unity2017_3) 
            {
                runtimeTypes  = resolver.Resolve<NativeTypeArray>("?ms_runtimeTypes@RTTI@@0URuntimeTypeArray@1@A");
                types = new List<RuntimeTypeInfo>(runtimeTypes->Count);
                for (int i = 0; i < runtimeTypes->Count; i++)
                {
                    var info = *(&runtimeTypes->First)[i];
                    types.Add(new RuntimeTypeInfo(info.V2));
                }
            } else
            {
                runtimeTypes = resolver.Resolve<NativeTypeArray>("?ms_runtimeTypes@RTTI@@2URuntimeTypeArray@1@A");
                types = new List<RuntimeTypeInfo>(runtimeTypes->Count);
                for (int i = 0; i < runtimeTypes->Count; i++)
                {
                    var info = *(&runtimeTypes->First)[i];
                    types.Add(new RuntimeTypeInfo(info.V1));
                }
            }

        }

        public IEnumerator<RuntimeTypeInfo> GetEnumerator()
        {
            return types.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return types.GetEnumerator();
        }

        unsafe struct NativeTypeArray
        {
            public int Count;
            public RuntimeTypeInfo.NativeTypeInfoUnion* First;
        }
    }
}
