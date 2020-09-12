using System;
using System.Runtime.InteropServices;

namespace Unity
{
    public class NativeObjectFactory
    {
        UnityVersion version;
        SymbolResolver resolver;

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

        readonly ProduceDelegateV1 s_ProduceV1;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr ProduceDelegateV1(ref byte a, int instanceID, MemLabelId label, ObjectCreationMode creationMode);

        readonly ProduceDelegateV2 s_ProduceV2;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr ProduceDelegateV2(ref byte a, ref byte b, int instanceID, MemLabelId label, ObjectCreationMode creationMode);

        readonly InstanceIDToObjectDelegate s_InstanceIDToObject;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr InstanceIDToObjectDelegate(int instanceID);

        readonly DestroyImmediateDelegate s_DestroyImmediate;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void DestroyImmediateDelegate(IntPtr objectPtr, bool allowDestroyingAssets);

        bool ScriptingBackendNativeObjectPtrOpaque => version >= UnityVersion.Unity2019_2;

        bool HasGetSceneVisibilityState => version >= UnityVersion.Unity2019_1;

        bool HasGetSpriteAtlasDatabase => version >= UnityVersion.Unity2017_1;

        bool IsObjectProduceV1 => version < UnityVersion.Unity2017_2;

        public NativeObjectFactory(UnityVersion version, SymbolResolver resolver)
        {
            this.version = version;
            this.resolver = resolver;
            if(HasGetSpriteAtlasDatabase) s_GetSpriteAtlasDatabase = resolver.ResolveFunction<GetSpriteAtlasDatabaseDelegate>("?GetSpriteAtlasDatabase@@YAAEAVSpriteAtlasDatabase@@XZ");
            if(HasGetSceneVisibilityState) s_GetSceneVisibilityState = resolver.ResolveFunction<GetSceneVisibilityStateDelegate>("?GetSceneVisibilityState@@YAAEAVSceneVisibilityState@@XZ");
            s_GetInspectorExpandedState = resolver.ResolveFunction<GetInspectorExpandedStateDelegate>("?GetInspectorExpandedState@@YAAEAVInspectorExpandedState@@XZ");
            s_GetAnnotationManager = resolver.ResolveFunction<GetAnnotationManagerDelegate>("?GetAnnotationManager@@YAAEAVAnnotationManager@@XZ");
            s_GetMonoManager = resolver.ResolveFunction<GetMonoManagerDelegate>("?GetMonoManager@@YAAEAVMonoManager@@XZ");
            if (IsObjectProduceV1)
            {
                s_ProduceV1 = resolver.ResolveFunction<ProduceDelegateV1>("?Produce@Object@@SAPEAV1@PEBVType@Unity@@HUMemLabelId@@W4ObjectCreationMode@@@Z");
            }
            else
            { 
                s_ProduceV2 = resolver.ResolveFunction<ProduceDelegateV2>("?Produce@Object@@CAPEAV1@PEBVType@Unity@@0HUMemLabelId@@W4ObjectCreationMode@@@Z");
            }
            if (ScriptingBackendNativeObjectPtrOpaque)
            {
                s_InstanceIDToObject = resolver.ResolveFunction<InstanceIDToObjectDelegate>("?EditorUtility_CUSTOM_InstanceIDToObject@@YAPEAVScriptingBackendNativeObjectPtrOpaque@@H@Z");
                s_DestroyImmediate = resolver.ResolveFunction<DestroyImmediateDelegate>("?Object_CUSTOM_DestroyImmediate@@YAXPEAVScriptingBackendNativeObjectPtrOpaque@@E@Z");
            }
            else
            {
                s_InstanceIDToObject = resolver.ResolveFunction<InstanceIDToObjectDelegate>("?EditorUtility_CUSTOM_InstanceIDToObject@@YAPEAUMonoObject@@H@Z");
                s_DestroyImmediate = resolver.ResolveFunction<DestroyImmediateDelegate>("?Object_CUSTOM_DestroyImmediate@@YAXPEAUMonoObject@@E@Z");
            }
        }

        public NativeObject GetSpriteAtlasDatabase()
        {
            return new NativeObject(s_GetSpriteAtlasDatabase(), this, PersistentTypeID.SpriteAtlasDatabase);
        }

        public NativeObject GetSceneVisibilityState()
        {
            return new NativeObject(s_GetSceneVisibilityState(), this, PersistentTypeID.SceneVisibilityState);
        }

        public NativeObject GetInspectorExpandedState()
        {
            return new NativeObject(s_GetInspectorExpandedState(), this, PersistentTypeID.InspectorExpandedState);
        }

        public NativeObject GetAnnotationManager()
        {
            return new NativeObject(s_GetAnnotationManager(), this, PersistentTypeID.AnnotationManager);
        }

        public NativeObject GetMonoManager()
        {
            return new NativeObject(s_GetMonoManager(), this, PersistentTypeID.MonoManager);
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

            // TODO: Why does this take two types?
            IntPtr ptr;
            if (IsObjectProduceV1)
            {
                ptr = s_ProduceV1(ref type.GetPinnableReference(), instanceID, new MemLabelId(), creationMode);
            } else
            {
                ptr = s_ProduceV2(ref type.GetPinnableReference(), ref type.GetPinnableReference(), instanceID, new MemLabelId(), creationMode);
            }
            if (ptr.ToInt64() == 0) return null;
            return new NativeObject(ptr, this, type.PersistentTypeID);
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
            if (obj == null) throw new InvalidOperationException("NativeObject is null");
            if (s_InstanceIDToObject == null) throw new InvalidOperationException("s_InstanceIDToObject is null");
            if (s_DestroyImmediate == null) throw new InvalidOperationException("s_DestroyImmediate is null");
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

            var managed = s_InstanceIDToObject(obj.InstanceID);
            s_DestroyImmediate(managed, false);
        }
    }
}
