using System;
using System.Reflection;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace TypeTreeDumper
{
    static unsafe class DiaSourceFactory
    {
        public static HRESULT CreateDiaSource(IDiaDataSource** ppvObject)
        {
            return CreateInstance(__uuidof<DiaSource>(), __uuidof<IDiaDataSource>(), (void**)ppvObject);
        }

        public static HRESULT CreateInstance(Guid* rclsid, Guid* riid, void** ppvObject)
        {
            using var factory = new ComPtr<IClassFactory>();
            HRESULT hr = DllGetClassObject(rclsid, __uuidof<IClassFactory>(), (void**)factory.GetAddressOf());

            if (hr.FAILED)
                return hr;

            return factory.Get()->CreateInstance(null, riid, ppvObject);
        }

        [DllImport("msdia", ExactSpelling = true)]
        public static extern HRESULT DllGetClassObject(Guid* rclsid, Guid* riid, void** ppv);

        [DllImport("msdia", ExactSpelling = true)]
        public static extern HRESULT DllCanUnloadNow();

        static DiaSourceFactory()
        {
            NativeLibrary.SetDllImportResolver(typeof(DiaSourceFactory).Assembly, DiaDllResolver);
        }

        static IntPtr DiaDllResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName != "msdia")
                return NativeLibrary.Load(libraryName, assembly, searchPath);

            if (Environment.Is64BitProcess)
                return NativeLibrary.Load("msdia140_amd64.dll", assembly, searchPath);
            else
                return NativeLibrary.Load("msdia140.dll", assembly, searchPath);
        }
    }
}
