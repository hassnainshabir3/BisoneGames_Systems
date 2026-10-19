namespace bison.core.Common
{
    public static class LogMessages
    {
        public const string SaveTamperDetected = "Save data was modified in memory. Reloading from disk.";
        public const string SplashConfigInvalid = "Splash config or next scene is missing.";
        public const string PlayerPrefabMissing = "Player prefab is not assigned.";
        public const string AudioLibraryMissing = "Audio library is not assigned.";

        public static string MissingResource(string resourcePath) => $"Missing Resources asset: {resourcePath}";
        public static string SceneNotInBuild(string sceneName) => $"Scene '{sceneName}' is not in Build Settings.";
        public static string DuplicateId(string identifier) => $"Duplicate id: {identifier}";
        public static string SaveWriteFailed(string reason) => $"Save write failed: {reason}";
        public static string SaveReadFailed(string reason) => $"Save read failed: {reason}";
        public static string SoundNotFound(string soundName) => $"Sound not found in library: {soundName}";
        public static string SoundHasNoClips(string soundName) => $"Sound has no clips: {soundName}";
    }
}
