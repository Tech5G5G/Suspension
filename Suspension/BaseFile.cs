namespace Suspension;

/// <summary>
/// Represents the base class for files in <see cref="Suspension"/>.
/// </summary>
public abstract class BaseFile
{
    /// <summary>
    /// Creates a new instance of the inherited <see cref="BaseFile"/> class.
    /// </summary>
    protected BaseFile() { }

    /// <summary>
    /// Creates a new instance of the inherited <see cref="BaseFile"/> class using a <see cref="Stream"/>.
    /// </summary>
    /// <param name="fileStream">A <see cref="Stream"/> that has read access to the inherited <see cref="BaseFile"/> type.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    protected BaseFile(Stream fileStream)
    {
        ArgumentNullException.ThrowIfNull(fileStream, nameof(fileStream));

        if (!fileStream.CanRead)
            throw new ArgumentException("Stream must have read access.", nameof(fileStream));
    }
}
