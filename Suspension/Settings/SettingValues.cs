namespace Suspension.Settings;

public static class SettingValues
{
    public static Setting<bool> StatusBar { get; } = new(nameof(StatusBar), true);

    public static Setting<bool> Airtimes { get; } = new(nameof(Airtimes), false);

    public static Setting<string> GeminiAPIKey { get; } = new(nameof(GeminiAPIKey), string.Empty);
}
