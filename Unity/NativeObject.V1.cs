using System;
using System.Collections.Specialized;

namespace Unity
{
    public partial class NativeObject
    {
        internal unsafe class V1 : INativeObjectImpl
        {
            NativeObject* nativeObject;

            public int InstanceID => nativeObject->InstanceID;

            public BitVector32 Bits => nativeObject->bits;

            public IntPtr Pointer => new IntPtr(nativeObject);

            public V1(IntPtr ptr)
            {
                nativeObject = (NativeObject*)ptr;
            }

            internal struct NativeObject
            {
                public IntPtr* VirtualFunctionTable;
                public int InstanceID;
                public BitVector32 bits;
                // There are more fields but they aren't needed.
            }
        }
    }
}
