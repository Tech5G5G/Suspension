using System.Collections;

namespace Suspension.SST;

/// <summary>
/// Represents an SST file.
/// </summary>
/// <remarks>Index is time since <see cref="Timestamp"/> times <see cref="SampleRate"/>.</remarks>
public partial class TelemetryFile : BaseFile, IEnumerable<(int Fork, int Shock)>
{
    /// <summary>
    /// Gets the size of the <see cref="TelemetryFile"/> in bytes.
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// Gets the sample rate of the <see cref="TelemetryFile"/> in Hz.
    /// </summary>
    public int SampleRate { get; }

    /// <summary>
    /// Gets the <see cref="DateTime"/> at which the <see cref="TelemetryFile"/> started being recorded.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the amount of data contained within the <see cref="TelemetryFile"/>.
    /// </summary>
    public int Count { get; }

    private readonly List<(int, int)> data = [];

    /// <summary>
    /// Creates a new instance of <see cref="TelemetryFile"/> using a <see cref="Stream"/>.
    /// </summary>
    /// <param name="fileStream">A <see cref="Stream"/> that has read access to an SST file.</param>
    /// <exception cref="InvalidDataException"/>
    /// <inheritdoc/>
    public TelemetryFile(Stream fileStream) : base(fileStream)
    {
        var bytes = ReadAllBytes(fileStream);

        if (bytes.Length < 3 || Encoding.UTF8.GetString(bytes[..3]) != "SST") //Check header of SST file
            throw new InvalidDataException("Incorrect file contents. It may be corrupt or of a different format.");

        //Populate properties from file and file header
        Size = fileStream.Length;
        SampleRate = BitConverter.ToUInt16(bytes, 4);
        Timestamp = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(bytes, 8)).ToLocalTime().DateTime;

        for (int i = 16; i < bytes.Length; i += 4)
            data.Add((
                BitConverter.ToUInt16(bytes, i),
                BitConverter.ToUInt16(bytes, i + 2)));

        //Cache count
        Count = data.Count;
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        if (stream is MemoryStream mStream)
            return mStream.ToArray();

        using MemoryStream memoryStream = new();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    #region IEnumerable

    public IEnumerator<(int, int)> GetEnumerator() => data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}
