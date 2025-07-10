namespace Suspension.SST;

/// <summary>
/// Represents an SST project (.sstproj) file.
/// </summary>
public class ProjectFile : BaseFile
{
    /// <summary>
    /// Gets or sets the path to the SST file used in this project.
    /// </summary>
    public string SSTPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the GPX file used in this project.
    /// </summary>
    public string GPXPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the video file used in this project.
    /// </summary>
    public string VideoPath { get; set; }

    /// <summary>
    /// Gets or sets the array of strings representing each map layer URL used in this project.
    /// </summary>
    public string[] Layers { get; set; }

    /// <summary>
    /// Gets or sets the path of the SST project file.
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="ProjectFile"/>.
    /// </summary>
    public ProjectFile() : base() { }

    /// <summary>
    /// Creates a new instance of <see cref="ProjectFile"/> using a <see cref="Stream"/>.
    /// </summary>
    /// <param name="fileStream">A <see cref="Stream"/> that has read access to an SST project file.</param>
    /// <param name="path">The file path to the provided <paramref name="fileStream"/>.</param>
    /// <inheritdoc/>
    public ProjectFile(Stream fileStream, string path) : base(fileStream)
    {
        var project = JsonSerializer.Deserialize<Project>(fileStream);

        FilePath = path;
        SSTPath = project.SST;
        GPXPath = project.GPX;
        VideoPath = project.Video;
        Layers = [.. project.Layers.Select(i => i.URL)];
    }

    /// <summary>
    /// Saves the file to the specified <paramref name="path"/>, replacing any files that exist.
    /// </summary>
    /// <param name="path">The path to save the <see cref="ProjectFile"/> to.</param>
    /// <returns>A <see cref="Task"/> representing the asyncronous write operation.</returns>
    public Task Save(string path) => File.WriteAllTextAsync(path, JsonSerializer.Serialize(new Project
    {
        SST = SSTPath,
        GPX = GPXPath,
        Video = VideoPath,
        Layers = [.. Layers.Select(i => new Layer { URL = i })]
    }));

    private class Project
    {
        [JsonPropertyName("sst")]
        public string SST { get; set; }

        [JsonPropertyName("video")]
        public string Video { get; set; }

        [JsonPropertyName("gpx")]
        public string GPX { get; set; }

        [JsonPropertyName("layers")]
        public Layer[] Layers { get; set; }
    }

    private class Layer
    {
        [JsonPropertyName("url")]
        public string URL { get; set; }
    }
}
