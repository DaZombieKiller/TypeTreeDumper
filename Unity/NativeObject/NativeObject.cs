using System;
using System.Collections.Specialized;

namespace Unity
{
    public partial class NativeObject : IDisposable
    {
        INativeObjectImpl nativeObject;

        NativeObjectFactory factory;

        PersistentTypeID persistentTypeID;

        public int InstanceID => nativeObject.InstanceID;

        public IntPtr Pointer => nativeObject.Pointer;

        public NativeObject(IntPtr ptr, NativeObjectFactory factory, PersistentTypeID persistentTypeID, UnityVersion version)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(ptr));

            if (version < UnityVersion.Unity5_0)
            {
                nativeObject = new V1(ptr);
            }
            else
            {
                nativeObject = new V5_0(ptr);
            }
            this.factory = factory;
            this.persistentTypeID = persistentTypeID;
        }

        public void Dispose()
        {
            if (nativeObject != null)
            {
                factory.DestroyIfNotSingletonOrPersistent(this, persistentTypeID);
            }
        }

        public byte TemporaryFlags => nativeObject.TemporaryFlags;

        public HideFlags HideFlags => nativeObject.HideFlags;

        public bool IsPersistent => nativeObject.IsPersistent;

        public uint CachedTypeIndex => nativeObject.CachedTypeIndex;

        interface INativeObjectImpl
        {
            int InstanceID { get; }
            IntPtr Pointer { get; }
            public byte TemporaryFlags { get;  }
            public HideFlags HideFlags { get; }
            public bool IsPersistent { get; }
            public uint CachedTypeIndex { get; }
        }
    }
}