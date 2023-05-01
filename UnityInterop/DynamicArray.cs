using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Unity
{
    unsafe struct DynamicArray
    {
        public IntPtr Ptr;
        public MemLabelId Label;
        public ulong Size;
        public ulong Capacity;
    }

    unsafe struct DynamicArray<T> : IReadOnlyList<T>
        where T : unmanaged
    {
        public T* Ptr;
        public MemLabelId Label;
        public ulong Size;
        public ulong Capacity;

        T IReadOnlyList<T>.this[int index] => Ptr[index];

        public ref T this[int index] => ref Ptr[index];

        public ref T this[ulong index] => ref Ptr[index];

        public int Count => (int)Size;

        public IEnumerator<T> GetEnumerator()
        {
            for (ulong i = 0; i < Size; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
