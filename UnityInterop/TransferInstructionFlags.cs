using System;

namespace Unity
{
    [Flags]
    public enum TransferInstructionFlags
    {
        None                                    = 0,
        ReadWriteFromSerializedFile             = 1 << 0,
        AssetMetaDataOnly                       = 1 << 1,
        HandleDrivenProperties                  = 1 << 2,
        LoadAndUnloadAssetsDuringBuild          = 1 << 3,
        SerializeDebugProperties                = 1 << 4,
        IgnoreDebugPropertiesForIndex           = 1 << 5,
        BuildPlayerOnlySerializeBuildProperties = 1 << 6,
        IsCloningObject                         = 1 << 7,
        SerializeGameRelease                    = 1 << 8,
        SwapEndianess                           = 1 << 9,
        ResolveStreamedResourceSources          = 1 << 10,
        DontReadObjectsFromDiskBeforeWriting    = 1 << 11,
        SerializeMonoReload                     = 1 << 12,
        DontRequireAllMetaFlags                 = 1 << 13,
        SerializeForPrefabSystem                = 1 << 14,
        WarnAboutLeakedObjects                  = 1 << 15,
        LoadPrefabAsScene                       = 1 << 16,
        SerializeCopyPasteTransfer              = 1 << 17,
        EditorPlayMode                          = 1 << 18,
        BuildResourceImage                      = 1 << 19,
        SerializeEditorMinimalScene             = 1 << 21,
        GenerateBakedPhysixMeshes               = 1 << 22,
        ThreadedSerialization                   = 1 << 23,
        IsBuiltinResourcesFile                  = 1 << 24,
        PerformUnloadDependencyTracking         = 1 << 25,
        DisableWriteTypeTree                    = 1 << 26,
        AutoreplaceEditorWindow                 = 1 << 27,
        DontCreateMonoBehaviourScriptWrapper    = 1 << 28,
        SerializeForInspector                   = 1 << 29,
        SerializedAssetBundleVersion            = 1 << 30,
        AllowTextSerialization                  = 1 << 31,
    }
}
