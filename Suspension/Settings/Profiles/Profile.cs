namespace Suspension.Settings.Profiles;

public class Profile : ICloneable
{
    #region Properties

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("color")]
    public int? Color { get; set; }

    [JsonPropertyName("default")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("divisor")]
    public double AngleDivisor { get; set; }

    [JsonPropertyName("fork")]
    public Dimensions ForkDimensions { get; set; }

    [JsonPropertyName("shock")]
    public Dimensions ShockDimensions { get; set; }

    #endregion

    /// <summary>
    /// Occurs after a profile has been changed; that is, after the <see cref="SaveProfile(Profile)"/> or <see cref="RemoveProfile(Guid)"/> method.
    /// </summary>
    public static event TypedEventHandler<Profile[], ProfileChangedEventArgs> ProfileChanged;

    private static Profile[] _profiles;

    private const string FileName = "profiles.json";

    private static readonly StorageFolder localFolder = ApplicationData.Current.LocalFolder;

    /// <summary>
    /// Gets the current <see cref="Array"/> of <see cref="Profile"/> saved on the disk.
    /// </summary>
    /// <returns>
    /// If no IO exceptions occur, an <see cref="Array"/> of <see cref="Profile"/>, asynchronously. Otherwise, <see langword="null"/>.
    /// </returns>
    public static async Task<Profile[]> GetProfilesAsync()
    {
        try
        {
            if (_profiles is not null)
                return _profiles;
            else if (!new FileInfo(Path.Join(localFolder.Path, FileName)).Exists)
            {
                var file = await localFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
                var array = Array.Empty<Profile>();

                await FileIO.WriteTextAsync(file, JsonSerializer.Serialize(array));
                return _profiles = array;
            }
            else
            {
                var file = await localFolder.GetFileAsync(FileName);
                using var stream = await file.OpenStreamForReadAsync();
                return _profiles = JsonSerializer.Deserialize<Profile[]>(stream);
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Saves the specified <paramref name="profile"/> to the disk.
    /// </summary>
    /// <remarks>
    /// If another <see cref="Profile"/> with the same <see cref="Id"/> as <paramref name="profile"/> already exists, it will be overwritten. Otherwise, <paramref name="profile"/> is appended to the end of the <see cref="Array"/>.
    /// </remarks>
    /// <param name="profile">The <see cref="Profile"/> to save to the disk.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous write operation.</returns>
    public async static Task SaveProfile(Profile profile)
    {
        if (await GetProfilesAsync() is not Profile[] profiles)
            return;

        if (profiles.FirstOrDefault(i => i.Id == profile.Id) is Profile currentProfile)
            profiles[Array.IndexOf(profiles, currentProfile)] = profile;
        else
            profiles = [.. profiles, profile];

        await TryWriteProfiles(profiles);
        ProfileChanged?.Invoke(_profiles, new(false, profile.Id, profile));
    }

    /// <summary>
    /// Removes a <see cref="Profile"/> from the disk using its <paramref name="id"/>.
    /// </summary>
    /// <remarks>If the <see cref="Profile"/> is not found, no write operation occurs.</remarks>
    /// <param name="id">The <see cref="Guid"/> of the <see cref="Profile"/> to remove.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous write operation.</returns>
    public async static Task RemoveProfile(Guid id)
    {
        if (await GetProfilesAsync() is not Profile[] profileArray)
            return;

        var profiles = profileArray.ToList();
        if (profiles.FirstOrDefault(i => i.Id == id) is Profile profile)
        {
            profiles.Remove(profile);

            await TryWriteProfiles([.. profiles]);
            ProfileChanged?.Invoke(_profiles, new(true, id, null));
        }
    }

    private async static Task TryWriteProfiles(Profile[] profiles)
    {
        try
        {
            var file = await localFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, JsonSerializer.Serialize(_profiles = profiles));
        }
        catch { }
    }

    public object Clone() => new Profile()
    {
        Name = Name,
        Id = Id,
        Icon = Icon,
        Color = Color,
        IsDefault = IsDefault,
        AngleDivisor = AngleDivisor,
        ForkDimensions = new()
        {
            SideA = ForkDimensions.SideA,
            SideB = ForkDimensions.SideB
        },
        ShockDimensions = new()
        {
            SideA = ShockDimensions.SideA,
            SideB = ShockDimensions.SideB
        }
    };
}

public class Dimensions
{
    [JsonPropertyName("a")]
    public double SideA { get; set; }

    [JsonPropertyName("b")]
    public double SideB { get; set; }
}

public class ProfileChangedEventArgs(bool removed, Guid id, Profile profile) : EventArgs
{
    /// <summary>
    /// Gets whether <see cref="Profile"/> was removed.
    /// </summary>
    /// <remarks>If <see langword="false"/>, properties changed.</remarks>
    public bool Removed { get; } = removed;

    /// <summary>
    /// Gets the <see cref="Profile.Id"/> of the <see cref="Profiles.Profile"/> changed.
    /// </summary>
    public Guid ProfileId { get; } = id;

    /// <summary>
    /// Gets the <see cref="Profiles.Profile"/> changed.
    /// </summary>
    /// <remarks>Will be <see langword="null"/> if <see cref="Removed"/> is <see langword="true"/>.</remarks>
    public Profile Profile { get; } = profile;
}
