namespace Suspension.Settings;

public static class SettingValues
{
    public static Setting<bool> StatusBar { get; } = new(nameof(StatusBar), true);
}
