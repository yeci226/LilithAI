namespace LilithAI;

public static class ConfigMigration
{
    public const string FileName = "LilithAI.cfg";
    public const string LegacyFileName = "tw.shawn.lilith.ai.cfg";

    public static string Prepare(string configDirectory)
    {
        var path = Path.Combine(configDirectory, FileName);
        var legacyPath = Path.Combine(configDirectory, LegacyFileName);
        if (!File.Exists(path) && File.Exists(legacyPath))
            File.Move(legacyPath, path);
        return path;
    }
}
