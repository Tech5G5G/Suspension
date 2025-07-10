namespace Suspension.SST;

/// <summary>
/// Represents an SST file.
/// </summary>
/// <remarks>Data can be accessed using the indexer.</remarks>
public class TelemetryFile : BaseFile
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
    /// Gets the <see cref="DateTime"/> at which the <see cref="TelemetryFile"/> was recorded.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the amount of data contained within the <see cref="TelemetryFile"/>.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Gets a tuple of <see cref="int"/>.
    /// </summary>
    /// <remarks><see cref="ValueTuple{T1, T2}.Item1"/> represents the fork. <see cref="ValueTuple{T1, T2}.Item2"/> represents the shock.</remarks>
    /// <param name="index">The timestamp at which to get the tuple at.</param>
    /// <returns>A tuple containing information about the fork and shock.</returns>
    public (int, int) this[int index] => data[index];

    private readonly Dictionary<int, (int, int)> data = [];

    /// <summary>
    /// Creates a new instance of <see cref="TelemetryFile"/> using a <see cref="Stream"/>.
    /// </summary>
    /// <param name="fileStream">A <see cref="Stream"/> that has read access to an SST file.</param>
    /// <exception cref="InvalidDataException"/>
    /// <inheritdoc/>
    public TelemetryFile(Stream fileStream) : base(fileStream)
    {
        var bytes = ReadAllBytes(fileStream);

        if (bytes.Length < 3 || System.Text.Encoding.UTF8.GetString(bytes[..3]) != "SST") //Check header of SST file
            throw new InvalidDataException("Incorrect file contents. It may be corrupt or of a different format.");

        //Populate properties from file and file header
        Size = fileStream.Length;
        SampleRate = BitConverter.ToUInt16(bytes, 4);
        Timestamp = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(bytes, 8)).ToLocalTime().DateTime;

        int counter = 0;
        for (int i = 16; i < bytes.Length; i += 4)
        {
            data.Add(
                counter++,
                (BitConverter.ToUInt16(bytes, i), BitConverter.ToUInt16(bytes, i + 2)));
        }

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
}
