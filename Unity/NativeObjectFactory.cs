using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unity
{
    public unsafe class NativeObjectFactory
    {
        UnityVersion version;
        SymbolResolver resolver;

        MemLabelId* kMemBaseObject;

        readonly GetSpriteAtlasDatabaseDelegate s_GetSpriteAtlasDatabase;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr GetSpriteAtlasDatabaseDelegate();

        readonly GetSceneVisibilityStateDelegate s_GetSceneVisibilityState;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr GetSceneVisibilityStateDelegate();

        readonly GetInspectorExpandedStateDelegate s_GetInspectorExpandedState;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr GetInspectorExpandedStateDelegate();

        readonly GetAnnotationManagerDelegate s_GetAnnotationManager;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr GetAnnotationManagerDelegate();

        readonly GetMonoManagerDelegate s_GetMonoManager;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr GetMonoManagerDelegate();

        readonly ProduceDelegateV3_4 s_ProduceV3_4;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr ProduceDelegateV3_4(int classID, int instanceID, IntPtr baseAllocator, ObjectCreationMode creationMode);

        readonly ProduceDelegateV3_5 s_ProduceV3_5;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr ProduceDelegateV3_5(int classID, int instanceID, MemLabelId label, ObjectCreationMode creationMode);

        readonly ProduceDelegateV5_5 s_ProduceV5_5;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr ProduceDelegateV5_5(ref byte a, int instanceID, MemLabelId label, ObjectCreationMode creationMode);

        readonly ProduceDelegateV2017_2 s_ProduceV2017_2;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr ProduceDelegateV2017_2(ref byte a, ref byte b, int instanceID, MemLabelId label, ObjectCreationMode creationMode);

        readonly InstanceIDToObjectDelegate s_InstanceIDToObject;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr InstanceIDToObjectDelegate(int instanceID);

        readonly DestroyImmediateDelegate s_DestroyImmediate;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void DestroyImmediateDelegate(IntPtr objectPtr, bool allowDestroyingAssets);

        readonly DestroyObjectFromScriptingImmediateDelegate s_DestroyObjectFromScriptingImmediate;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void DestroyObjectFromScriptingImmediateDelegate(IntPtr objectPtr, bool allowDestroyingAssets);

        bool HasGetSceneVisibilityState => version >= UnityVersion.Unity2019_1;

        bool HasGetSpriteAtlasDatabase => version >= UnityVersion.Unity2017_1;

        public NativeObjectFactory(UnityVersion version, SymbolResolver resolver)
        {
            this.version = version;
            this.resolver = resolver;
            if(HasGetSpriteAtlasDatabase) s_GetSpriteAtlasDatabase = resolver.ResolveFunction<GetSpriteAtlasDatabaseDelegate>("?GetSpriteAtlasDatabase@@YAAEAVSpriteAtlasDatabase@@XZ");
            if(HasGetSceneVisibilityState) s_GetSceneVisibilityState = resolver.ResolveFunction<GetSceneVisibilityStateDelegate>("?GetSceneVisibilityState@@YAAEAVSceneVisibilityState@@XZ");
            s_GetInspectorExpandedState = resolver.ResolveFunction<GetInspectorExpandedStateDelegate>(
                "?GetInspectorExpandedState@@YAAEAVInspectorExpandedState@@XZ",
                "?GetInspectorExpandedState@@YAAAVInspectorExpandedState@@XZ");
            s_GetInspectorExpandedState = resolver.ResolveFunction<GetInspectorExpandedStateDelegate>(
                "?GetInspectorExpandedState@@YAAEAVInspectorExpandedState@@XZ",
                "?GetInspectorExpandedState@@YAAAVInspectorExpandedState@@XZ");
            s_GetAnnotationManager = resolver.ResolveFunction<GetAnnotationManagerDelegate>(
                "?GetAnnotationManager@@YAAEAVAnnotationManager@@XZ",
                "?GetAnnotationManager@@YAAAVAnnotationManager@@XZ");
            s_GetMonoManager = resolver.ResolveFunction<GetMonoManagerDelegate>(
                "?GetMonoManager@@YAAEAVMonoManager@@XZ",
                "?GetMonoManager@@YAAAVMonoManager@@XZ");
            if (version < UnityVersion.Unity5_5)
            {
                s_ProduceV3_4 = resolver.ResolveFunction<ProduceDelegateV3_4>("?Produce@Object@@SAPAV1@HHPAVBaseAllocator@@W4ObjectCreationMode@@@Z");
            }
            else if (version < UnityVersion.Unity5_5)
            {
                s_ProduceV3_5 = resolver.ResolveFunction<ProduceDelegateV3_5>(
                    "?Produce@Object@@SAPEAV1@HHUMemLabelId@@W4ObjectCreationMode@@@Z",
                    "?Produce@Object@@SAPAV1@HHUMemLabelId@@W4ObjectCreationMode@@@Z"
                    );
            }
            else if(version < UnityVersion.Unity2017_2)
            {
                s_ProduceV5_5 = resolver.ResolveFunction<ProduceDelegateV5_5>("?Produce@Object@@SAPEAV1@PEBVType@Unity@@HUMemLabelId@@W4ObjectCreationMode@@@Z");
            }
            else
            {
                s_ProduceV2017_2 = resolver.ResolveFunction<ProduceDelegateV2017_2>("?Produce@Object@@CAPEAV1@PEBVType@Unity@@0HUMemLabelId@@W4ObjectCreationMode@@@Z");
            }

            if(version < UnityVersion.Unity5_5)
            {
                s_DestroyObjectFromScriptingImmediate = resolver.ResolveFunction<DestroyObjectFromScriptingImmediateDelegate>(
                    "?DestroyObjectFromScriptingImmediate@Scripting@@YAXPEAVObject@@_N@Z",
                    "?DestroyObjectFromScriptingImmediate@Scripting@@YAXPAVObject@@_N@Z",
                    "?DestroyObjectFromScriptingImmediate@@YAXPAVObject@@_N@Z");
            }
            else if (version < UnityVersion.Unity2019_2)
            {
                s_InstanceIDToObject = resolver.ResolveFunction<InstanceIDToObjectDelegate>("?EditorUtility_CUSTOM_InstanceIDToObject@@YAPEAUMonoObject@@H@Z");
                s_DestroyImmediate = resolver.ResolveFunction<DestroyImmediateDelegate>("?Object_CUSTOM_DestroyImmediate@@YAXPEAUMonoObject@@E@Z");
            }
            else
            {
                s_InstanceIDToObject = resolver.ResolveFunction<InstanceIDToObjectDelegate>("?EditorUtility_CUSTOM_InstanceIDToObject@@YAPEAVScriptingBackendNativeObjectPtrOpaque@@H@Z");
                s_DestroyImmediate = resolver.ResolveFunction<DestroyImmediateDelegate>("?Object_CUSTOM_DestroyImmediate@@YAXPEAVScriptingBackendNativeObjectPtrOpaque@@E@Z");
            }
            if (version >= UnityVersion.Unity3_5)
            {
                kMemBaseObject = resolver.Resolve<MemLabelId>(
                    "?kMemBaseObject@@3UMemLabelId@@A",
                    "?kMemBaseObject@@3UkMemBaseObjectStruct@@A");
            }
        }

        public NativeObject GetSpriteAtlasDatabase()
        {
            return new NativeObject(s_GetSpriteAtlasDatabase(), this, PersistentTypeID.SpriteAtlasDatabase, version);
        }

        public NativeObject GetSceneVisibilityState()
        {
            return new NativeObject(s_GetSceneVisibilityState(), this, PersistentTypeID.SceneVisibilityState, version);
        }

        public NativeObject GetInspectorExpandedState()
        {
            return new NativeObject(s_GetInspectorExpandedState(), this, PersistentTypeID.InspectorExpandedState, version);
        }

        public NativeObject GetAnnotationManager()
        {
            return new NativeObject(s_GetAnnotationManager(), this, PersistentTypeID.AnnotationManager, version);
        }

        public NativeObject GetMonoManager()
        {
            return new NativeObject(s_GetMonoManager(), this, PersistentTypeID.MonoManager, version);
        }

        public NativeObject Produce(in RuntimeTypeInfo type, int instanceID, ObjectCreationMode creationMode)
        {
            // TODO: Support producing abstract types. To do this, the following steps are necessary:
            //       1. Replace T::VirtualRedirectTransfer with T::Transfer. This can be done by either
            //          hooking the method via EasyHook, or modifying the virtual function table.
            //          This works because both methods have compatible signatures.
            //       2. Create a new Factory method for the type, by locating its constructor function
            //          and using that to create a new delegate.
            //       3. Create a new RuntimeTypeInfo based on the original, with the new Factory method.
            //          It also needs to have the IsAbstract field set to false.
            //       4. Hook T::GetTypeVirtualInternal to return the appropriate RuntimeTypeInfo.
            if (type.IsAbstract)
                return null;

            IntPtr ptr;
            if (version < UnityVersion.Unity3_5)
            {
                ptr = s_ProduceV3_4((int)type.PersistentTypeID, instanceID, IntPtr.Zero, creationMode);
            }
            else if (version < UnityVersion.Unity5_5)
            {
                ptr = s_ProduceV3_5((int)type.PersistentTypeID, instanceID, *kMemBaseObject, creationMode);
            }
            else if (version < UnityVersion.Unity2017_2)
            {
                ptr = s_ProduceV5_5(ref type.GetPinnableReference(), instanceID, *kMemBaseObject, creationMode);
            }
            else
            {
                // TODO: Why does this take two types?
                ptr = s_ProduceV2017_2(ref type.GetPinnableReference(), ref type.GetPinnableReference(), instanceID, *kMemBaseObject, creationMode);
            }
            if (ptr == IntPtr.Zero) return null;
            return new NativeObject(ptr, this, type.PersistentTypeID, version);
        }

        public NativeObject GetOrProduce(in RuntimeTypeInfo type) => type.PersistentTypeID switch
        {
            PersistentTypeID.SpriteAtlasDatabase => GetSpriteAtlasDatabase(),
            PersistentTypeID.SceneVisibilityState => GetSceneVisibilityState(),
            PersistentTypeID.InspectorExpandedState => GetInspectorExpandedState(),
            PersistentTypeID.AnnotationManager => GetAnnotationManager(),
            PersistentTypeID.MonoManager => GetMonoManager(),
            _ => Produce(type, 0, ObjectCreationMode.Default),
        };

        public void DestroyIfNotSingletonOrPersistent(NativeObject obj, PersistentTypeID persistentTypeID)
        {
            if (obj.IsPersistent)
                return;

            switch (persistentTypeID)
            {
                case PersistentTypeID.SpriteAtlasDatabase:
                case PersistentTypeID.SceneVisibilityState:
                case PersistentTypeID.InspectorExpandedState:
                case PersistentTypeID.AnnotationManager:
                case PersistentTypeID.MonoManager:
                case PersistentTypeID.AssetBundle:
                    return;
            }
            if (version < UnityVersion.Unity5_5)
            {
                s_DestroyObjectFromScriptingImmediate(obj.Pointer, false);
            }
            else
            {
                var managed = s_InstanceIDToObject(obj.InstanceID);
                s_DestroyImmediate(managed, false);
            }
        }
    }
}
