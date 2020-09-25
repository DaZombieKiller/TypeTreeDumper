using System;
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

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        unsafe delegate char* CStrDelegate(IntPtr @this);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate IntPtr ClassIDToRTTI(int classID);

        public RuntimeTypeInfo this[int index] => types[index];

        public int Count => types.Count;

        unsafe public RuntimeTypeArray(UnityVersion version, SymbolResolver resolver)
        {
            NativeTypeArray* runtimeTypes;
            if (version >= UnityVersion.Unity5_5)
            {
                if (version >= UnityVersion.Unity2017_3)
                {
                    runtimeTypes = resolver.Resolve<NativeTypeArray>("?ms_runtimeTypes@RTTI@@0URuntimeTypeArray@1@A");
                }
                else
                {
                    runtimeTypes = resolver.Resolve<NativeTypeArray>("?ms_runtimeTypes@RTTI@@2URuntimeTypeArray@1@A");
                }

                types = new List<RuntimeTypeInfo>(runtimeTypes->Count);
                for (int i = 0; i < runtimeTypes->Count; i++)
                {
                    var info = (&runtimeTypes->First)[i];
                    types.Add(new RuntimeTypeInfo(info, resolver, version));
                }
            }
            else 
            {
                ClassIDToRTTI ClassIDToRTTI;
                if(version >= UnityVersion.Unity5_4)
                {
                    ClassIDToRTTI = resolver.ResolveFunction<ClassIDToRTTI>("?ClassIDToRTTI@Object@@SAPEAURTTI@@H@Z");
                }
                else if (version >= UnityVersion.Unity5_0)
                {
                    ClassIDToRTTI = resolver.ResolveFunction<ClassIDToRTTI>("?ClassIDToRTTI@Object@@SAPEAURTTI@1@H@Z");
                } else
                {
                    ClassIDToRTTI = resolver.ResolveFunction<ClassIDToRTTI>("?ClassIDToRTTI@Object@@SAPAURTTI@1@H@Z");
                }

                types = new List<RuntimeTypeInfo>();
                for(int i = 0; i < 2000; i++)
                {
                    var info = ClassIDToRTTI(i);
                    if(info != IntPtr.Zero)
                    {
                        types.Add(new RuntimeTypeInfo(info, resolver, version));
                    }
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
            public IntPtr First;
        }
    }
}
