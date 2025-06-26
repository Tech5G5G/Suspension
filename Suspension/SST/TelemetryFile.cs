namespace Suspension.SST;

/// <summary>
/// Represents an SST file.
/// </summary>
/// <remarks>Data can be accessed using the indexer.</remarks>
public class TelemetryFile
{
    private readonly Dictionary<int, (int, int)> data = [];

    /// <summary>
    /// Creates a new instance of <see cref="TelemetryFile"/> using a <see cref="Stream"/>.
    /// </summary>
    /// <param name="fileStream">A <see cref="Stream"/> that has read access to an SST file.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="InvalidDataException"/>
    public TelemetryFile(Stream fileStream)
    {
        if (!fileStream.CanRead)
            throw new ArgumentException("Stream must have read access.", nameof(fileStream));

        var bytes = ReadAllBytes(fileStream);

        //TODO: Get sampling rate of SST file

        if (bytes.Length < 3 || System.Text.Encoding.UTF8.GetString(bytes[..3]) != "SST") //Check header of SST file
            throw new InvalidDataException("Incorrect file contents. It may be corrupt or of a different format.");

        int counter = 0;
        for (int i = 16; i < bytes.Length; i += 4)
        {
            data.Add(
                counter++,
                (BitConverter.ToUInt16(bytes, i), BitConverter.ToUInt16(bytes, i + 2)));
        }

        size = fileStream.Length;
    }

    /// <summary>
    /// Gets the amount of data contained within the <see cref="TelemetryFile"/>.
    /// </summary>
    public int Count => data.Count;

    /// <summary>
    /// Gets the size of the SST file represented by the <see cref="TelemetryFile"/> in bytes.
    /// </summary>
    public long Size => size;
    private readonly long size;

    /// <summary>
    /// Gets a tuple of <see cref="int"/>.
    /// </summary>
    /// <remarks><see cref="ValueTuple{T1, T2}.Item1"/> represents the fork. <see cref="ValueTuple{T1, T2}.Item2"/> represents the shock.</remarks>
    /// <param name="index">The timestamp at which to get the tuple at.</param>
    /// <returns>A tuple containing information about the fork and shock.</returns>
    public (int, int) this[int index] => data[index];

    private static byte[] ReadAllBytes(Stream stream)
    {
        if (stream is MemoryStream mStream)
            return mStream.ToArray();

        using MemoryStream memoryStream = new();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Creates a <see cref="Stream"/> from a <paramref name="path"/> to an SST file.
    /// </summary>
    /// <remarks>The returned <see cref="Stream"/> can be used in the <see cref="TelemetryFile(Stream)"/> constructor.</remarks>
    /// <param name="path">A path to an SST file.</param>
    /// <returns>A <see cref="Stream"/> created from <paramref name="path"/>, asynchronously.</returns>
    /// <exception cref="ArgumentException"/>
    public static async Task<Stream> StreamFromPath(string path)
    {
        if (!new FileInfo(path).Extension.Equals(".sst", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must be of file type SST.", nameof(path));

        var file = await StorageFile.GetFileFromPathAsync(path);
        return await file.OpenStreamForReadAsync();
    }
}
