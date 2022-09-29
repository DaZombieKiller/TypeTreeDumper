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

        readonly delegate* unmanaged[Cdecl]<void*> s_GetSpriteAtlasDatabase;

        readonly delegate* unmanaged[Cdecl]<void*> s_GetSceneVisibilityState;

        readonly delegate* unmanaged[Cdecl]<void*> s_GetInspectorExpandedState;

        readonly delegate* unmanaged[Cdecl]<void*> s_GetAnnotationManager;

        readonly delegate* unmanaged[Cdecl]<void*> s_GetMonoManager;

        readonly delegate* unmanaged[Cdecl]<int, int, void*, ObjectCreationMode, void*> s_ProduceV3_4;

        readonly delegate* unmanaged[Cdecl]<int, int, MemLabelId, ObjectCreationMode, void*> s_ProduceV3_5;

        readonly delegate* unmanaged[Cdecl]<byte*, int, MemLabelId, ObjectCreationMode, void*> s_ProduceV5_5;

        readonly delegate* unmanaged[Cdecl]<byte*, byte*, int, MemLabelId, ObjectCreationMode, void*> s_ProduceV2017_2;

        readonly delegate* unmanaged[Cdecl]<int, byte*> s_InstanceIDToObject;

        readonly delegate* unmanaged[Cdecl]<void*, byte, void> s_DestroyImmediate;

        readonly delegate* unmanaged[Cdecl]<void*, byte, void> s_DestroyObjectFromScriptingImmediate;

        bool HasGetSceneVisibilityState => version >= UnityVersion.Unity2019_1;

        bool HasGetSpriteAtlasDatabase => version >= UnityVersion.Unity2017_1;

        public NativeObjectFactory(UnityVersion version, SymbolResolver resolver)
        {
            this.version  = version;
            this.resolver = resolver;
            
            if (HasGetSpriteAtlasDatabase)
                s_GetSpriteAtlasDatabase = (delegate* unmanaged[Cdecl]<void*>)resolver.Resolve($"?GetSpriteAtlasDatabase@@YAA{NameMangling.Ptr64}AVSpriteAtlasDatabase@@XZ");

            if (HasGetSceneVisibilityState)
                s_GetSceneVisibilityState = (delegate* unmanaged[Cdecl]<void*>)resolver.Resolve($"?GetSceneVisibilityState@@YAA{NameMangling.Ptr64}AVSceneVisibilityState@@XZ");

            s_GetInspectorExpandedState = (delegate* unmanaged[Cdecl]<void*>)resolver.Resolve($"?GetInspectorExpandedState@@YAA{NameMangling.Ptr64}AVInspectorExpandedState@@XZ");
            s_GetInspectorExpandedState = (delegate* unmanaged[Cdecl]<void*>)resolver.Resolve($"?GetInspectorExpandedState@@YAA{NameMangling.Ptr64}AVInspectorExpandedState@@XZ");
            s_GetAnnotationManager      = (delegate* unmanaged[Cdecl]<void*>)resolver.Resolve($"?GetAnnotationManager@@YAA{NameMangling.Ptr64}AVAnnotationManager@@XZ");
            s_GetMonoManager            = (delegate* unmanaged[Cdecl]<void*>)resolver.Resolve($"?GetMonoManager@@YAA{NameMangling.Ptr64}AVMonoManager@@XZ");

            if (version < UnityVersion.Unity3_5)
                s_ProduceV3_4 = (delegate* unmanaged[Cdecl]<int, int, void*, ObjectCreationMode, void*>)resolver.Resolve($"?Produce@Object@@SAP{NameMangling.Ptr64}AV1@HHP{NameMangling.Ptr64}AVBaseAllocator@@W4ObjectCreationMode@@@Z");
            else if (version < UnityVersion.Unity5_5)
                s_ProduceV3_5 = (delegate* unmanaged[Cdecl]<int, int, MemLabelId, ObjectCreationMode, void*>)resolver.Resolve($"?Produce@Object@@SAP{NameMangling.Ptr64}AV1@HHUMemLabelId@@W4ObjectCreationMode@@@Z");
            else if(version < UnityVersion.Unity2017_2)
                s_ProduceV5_5 = (delegate* unmanaged[Cdecl]<byte*, int, MemLabelId, ObjectCreationMode, void*>)resolver.Resolve($"?Produce@Object@@SAP{NameMangling.Ptr64}AV1@P{NameMangling.Ptr64}BVType@Unity@@HUMemLabelId@@W4ObjectCreationMode@@@Z");
            else
                s_ProduceV2017_2 = (delegate* unmanaged[Cdecl]<byte*, byte*, int, MemLabelId, ObjectCreationMode, void*>)resolver.Resolve($"?Produce@Object@@CAP{NameMangling.Ptr64}AV1@P{NameMangling.Ptr64}BVType@Unity@@0HUMemLabelId@@W4ObjectCreationMode@@@Z");

            if (version < UnityVersion.Unity5_5)
            {
                s_DestroyObjectFromScriptingImmediate = (delegate* unmanaged[Cdecl]<void*, byte, void>)resolver.Resolve(
                    $"?DestroyObjectFromScriptingImmediate@Scripting@@YAXP{NameMangling.Ptr64}AVObject@@_N@Z",
                    $"?DestroyObjectFromScriptingImmediate@@YAXP{NameMangling.Ptr64}AVObject@@_N@Z"
                );
            }
            else if (version < UnityVersion.Unity2019_2)
            {
                s_InstanceIDToObject = (delegate* unmanaged[Cdecl]<int, byte*>)resolver.Resolve($"?EditorUtility_CUSTOM_InstanceIDToObject@@YAP{NameMangling.Ptr64}AUMonoObject@@H@Z");
                s_DestroyImmediate   = (delegate* unmanaged[Cdecl]<void*, byte, void>)resolver.Resolve(
                    $"?Object_CUSTOM_DestroyImmediate@@YAXP{NameMangling.Ptr64}AUMonoObject@@E@Z",
                    $"?Object_CUSTOM_DestroyImmediate@@YAXP{NameMangling.Ptr64}AUMonoObject@@_N@Z");
            }
            else
            {
                s_InstanceIDToObject = (delegate* unmanaged[Cdecl]<int, byte*>)resolver.Resolve($"?EditorUtility_CUSTOM_InstanceIDToObject@@YAP{NameMangling.Ptr64}AVScriptingBackendNativeObjectPtrOpaque@@H@Z");
                s_DestroyImmediate   = (delegate* unmanaged[Cdecl]<void*, byte, void>)resolver.Resolve($"?Object_CUSTOM_DestroyImmediate@@YAXP{NameMangling.Ptr64}AVScriptingBackendNativeObjectPtrOpaque@@E@Z");
            }

            if (version >= UnityVersion.Unity3_5 && version < UnityVersion.Unity2022_2)
            {
                kMemBaseObject = resolver.Resolve<MemLabelId>(
                    "?kMemBaseObject@@3UMemLabelId@@A",
                    "?kMemBaseObject@@3UkMemBaseObjectStruct@@A"
                );
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

            void* ptr;
            if (version < UnityVersion.Unity3_5)
            {
                ptr = s_ProduceV3_4((int)type.PersistentTypeID, instanceID, null, creationMode);
            }
            else if (version < UnityVersion.Unity5_5)
            {
                ptr = s_ProduceV3_5((int)type.PersistentTypeID, instanceID, *kMemBaseObject, creationMode);
            }
            else if (version < UnityVersion.Unity2017_2)
            {
                fixed (byte* typePtr = &type.GetPinnableReference())
                    ptr = s_ProduceV5_5(typePtr, instanceID, *kMemBaseObject, creationMode);
            }
            else
            {
                MemLabelId labelId = version < UnityVersion.Unity2022_2 ? *kMemBaseObject : MemLabelId.DefaultMemBaseObject_2020_2_6;
                // TODO: Why does this take two types?
                fixed (byte* typePtr = &type.GetPinnableReference())
                    ptr = s_ProduceV2017_2(typePtr, typePtr, instanceID, labelId, creationMode);
            }
            if (ptr == null) return null;
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
                s_DestroyObjectFromScriptingImmediate(obj.Pointer, 0);
            }
            else
            {
                var managed = s_InstanceIDToObject(obj.InstanceID);
                s_DestroyImmediate(managed, 0);
            }
        }
    }
}
