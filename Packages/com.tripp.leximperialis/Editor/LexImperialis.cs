using System;
using System.Collections.Generic;
using UnityEngine;

namespace TRIPP.LexImperialis.Editor
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
        public Judicator judicator;
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