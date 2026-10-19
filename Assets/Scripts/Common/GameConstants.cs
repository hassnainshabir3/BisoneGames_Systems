using UnityEngine;

namespace bison.core.Common
{
    public static class ResourcePaths
    {
        public const string Catalog = "bison/GameCatalog";
        public const string AudioManager = "bison/AudioManager";
        public const string TransitionMaterial = "bison/TransitionMaterial";
    }

    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
    }

    public static class ObjectTags
    {
        public const string Player = "Player";
        public const string MainCamera = "MainCamera";
    }

    public static class SaveFiles
    {
        public const string FileName = "bison.sav";
        public const string BackupSuffix = ".bak";
        public const string TemporarySuffix = ".tmp";
    }

    public static class ShaderProperties
    {
        public static int BaseColor { get; } = Shader.PropertyToID("_BaseColor");
        public static int LegacyColor { get; } = Shader.PropertyToID("_Color");
        public static int Progress { get; } = Shader.PropertyToID("_Progress");
        public static int Style { get; } = Shader.PropertyToID("_Style");
        public static int Reveal { get; } = Shader.PropertyToID("_Reveal");
        public static int Seed { get; } = Shader.PropertyToID("_Seed");
    }
}
