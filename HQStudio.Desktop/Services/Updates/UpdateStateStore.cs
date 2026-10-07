using System.IO;
using System.Text.Json;

namespace HQStudio.Services.Updates
{
    public sealed class UpdateState
    {
        public DateTime? LastCheckUtc { get; set; }
        public string? LastLatestTag { get; set; }
    }

    /// <summary>Remembers when updates were last checked so the startup check runs at most once per interval.</summary>
    public sealed class UpdateStateStore
    {
        private readonly string _path;

        public UpdateStateStore(string? path = null)
        {
            _path = path ?? DefaultPath;
        }

        public static string DefaultPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HQStudio", "update-state.json");

        public UpdateState Load()
        {
            try
            {
                if (File.Exists(_path))
                    return JsonSerializer.Deserialize<UpdateState>(File.ReadAllText(_path)) ?? new UpdateState();
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException) { }
            return new UpdateState();
        }

        public void Save(UpdateState state)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllText(_path, JsonSerializer.Serialize(state));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }

    public static class UpdateSettingsReader
    {
        public static string DefaultSettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HQStudio", "settings.json");

        /// <summary>
        /// Reads "AutoCheckUpdates" from settings.json; missing file, missing key or bad JSON mean enabled.
        /// SettingsService does not model this key, so it is read directly.
        /// </summary>
        public static bool IsAutoCheckEnabled(string? settingsPath = null)
        {
            try
            {
                settingsPath ??= DefaultSettingsPath;
                if (!File.Exists(settingsPath))
                    return true;

                using var doc = JsonDocument.Parse(File.ReadAllText(settingsPath));
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in doc.RootElement.EnumerateObject())
                    {
                        if (string.Equals(p.Name, "AutoCheckUpdates", StringComparison.OrdinalIgnoreCase) &&
                            p.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            return p.Value.GetBoolean();
                    }
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException) { }
            return true;
        }
    }
}
