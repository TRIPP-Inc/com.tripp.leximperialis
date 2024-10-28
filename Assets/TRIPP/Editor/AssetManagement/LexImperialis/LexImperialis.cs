using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace TRIPP.Editor.AssetManagement
{
    public class LexImperialis : ScriptableObject
    {
        public List<JudicatorFilter> judicatorFilters;
    }

    [Serializable]
    public class JudicatorFilter
    {
        public string objectType;
        public ImporterType importerType;
        public UnityEngine.Object judicator;
    }

    public enum ImporterType
    {
        AssetImporter,
        AudioImporter,
        ComputeShaderImporter,
        GUISkin,
        ModelImporter,
        MonoImporter,
        PrefabImporter,
        ShaderGraphImporter,
        ShaderImporter,
        TextureImporter,
        TrueTypeFontImporter,
        VideoClipImporter
    }
}