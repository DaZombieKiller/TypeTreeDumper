using System;
using System.Runtime.InteropServices;

namespace TypeTreeDumper
{
    public struct DetourHook : IDisposable
    {
        public IntPtr Pointer;
            
        public IntPtr DetourPointer;

        public static DetourHook Create<T>(IntPtr pointer, T detour)
            where T : Delegate
        {
            var hook = new DetourHook
            {
                Pointer       = pointer,
                DetourPointer = Marshal.GetFunctionPointerForDelegate(detour)
            };

            Detour.TransactionBegin();
            Detour.UpdateThread(Kernel32.GetCurrentThread());
            Detour.Attach(ref hook.Pointer, hook.DetourPointer);
            Detour.TransactionCommit();
            return hook;
        }

        public void Dispose()
        {
            if (Pointer == IntPtr.Zero || DetourPointer == IntPtr.Zero)
                return;

            Detour.TransactionBegin();
            Detour.UpdateThread(Kernel32.GetCurrentThread());
            Detour.Detach(ref Pointer, DetourPointer);
            Detour.TransactionCommit();
            Pointer       = IntPtr.Zero;
            DetourPointer = IntPtr.Zero;
        }
    }

    public static class Detour
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate int DetourTransactionBeginDelegate();
        static readonly DetourTransactionBeginDelegate s_DetourTransactionBegin;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate int DetourTransactionCommitDelegate();
        static readonly DetourTransactionCommitDelegate s_DetourTransactionCommit;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate int DetourUpdateThreadDelegate(IntPtr hThread);
        static readonly DetourUpdateThreadDelegate s_DetourUpdateThread;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate int DetourAttachDelegate(ref IntPtr ppPointer, IntPtr pDetour);
        static readonly DetourAttachDelegate s_DetourAttach;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate int DetourDetachDelegate(ref IntPtr ppPointer, IntPtr pDetour);
        static readonly DetourDetachDelegate s_DetourDetach;

        static Detour()
        {
            IntPtr module;

            if (Environment.Is64BitProcess)
                module = Kernel32.LoadLibrary("detours64.dll");
            else
                module = Kernel32.LoadLibrary("detours32.dll");

            s_DetourTransactionBegin  = Kernel32.GetProcAddress<DetourTransactionBeginDelegate>(module, "DetourTransactionBegin");
            s_DetourTransactionCommit = Kernel32.GetProcAddress<DetourTransactionCommitDelegate>(module, "DetourTransactionCommit");
            s_DetourUpdateThread      = Kernel32.GetProcAddress<DetourUpdateThreadDelegate>(module, "DetourUpdateThread");
            s_DetourAttach            = Kernel32.GetProcAddress<DetourAttachDelegate>(module, "DetourAttach");
            s_DetourDetach            = Kernel32.GetProcAddress<DetourDetachDelegate>(module, "DetourDetach");
        }

        public static int TransactionBegin() => s_DetourTransactionBegin.Invoke();

        public static int TransactionCommit() => s_DetourTransactionCommit.Invoke();

        public static int UpdateThread(IntPtr thread) => s_DetourUpdateThread(thread);

        public static int Attach(ref IntPtr pointer, IntPtr detour) => s_DetourAttach(ref pointer, detour);

        public static int Detach(ref IntPtr pointer, IntPtr detour) => s_DetourDetach(ref pointer, detour);
    }
}
