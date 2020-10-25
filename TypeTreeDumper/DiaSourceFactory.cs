using System;
using System.Runtime.InteropServices;
using Dia2Lib;

namespace TypeTreeDumper
{
    static class DiaSourceFactory
    {
        static readonly IntPtr s_Module;

        static readonly DllGetClassObjectDelegate s_DllGetClassObject;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate void DllGetClassObjectDelegate(
            in Guid classId,
            in Guid interfaceId,
            [MarshalAs(UnmanagedType.IUnknown)] out object @object
        );

        static DiaSourceFactory()
        {
            if (Environment.Is64BitProcess)
                s_Module = Kernel32.LoadLibrary("msdia140_amd64.dll");
            else
                s_Module = Kernel32.LoadLibrary("msdia140.dll");

            s_DllGetClassObject = Kernel32.GetProcAddress<DllGetClassObjectDelegate>(s_Module, "DllGetClassObject");
        }

        static void DllGetClassObject<T>(in Guid classId, out T @object)
            where T : class
        {
            s_DllGetClassObject(classId, typeof(T).GUID, out var box);
            @object = box as T;
        }

        public static IDiaDataSource CreateInstance()
        {
            DllGetClassObject(
                typeof(DiaSourceClass).GUID,
                out IClassFactory factory
            );

            factory.CreateInstance(null, out IDiaDataSource source);
            return source;
        }
    }
}
