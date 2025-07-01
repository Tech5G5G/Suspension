namespace Suspension.Settings;

public static class SettingValues
{
    public static Setting<bool> StatusBar { get; } = new(nameof(StatusBar), true);

    public static Setting<string> BaseMapLayer { get; } = new(nameof(BaseMapLayer), "https://mts1.google.com/vt/lyrs=y&x={x}&y={y}&z={z}");
}
