using System;

namespace Unity
{
    unsafe struct DynamicArray
    {
        public IntPtr Ptr;
        public MemLabelId Label;
        public ulong Size;
        public ulong Capacity;
    }

    unsafe struct DynamicArray<T>
        where T : unmanaged
    {
        public T* Ptr;
        public MemLabelId Label;
        public ulong Size;
        public ulong Capacity;
    }
}
