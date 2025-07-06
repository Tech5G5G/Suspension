using System.Xml.Serialization;

namespace Suspension.GPS;

/// <summary>
/// Represents a GPX file.
/// </summary>
public class GPXFile
{
    /// <summary>
    /// Creates a new instance of <see cref="GPXFile"/> using a <see cref="Stream"/>.
    /// </summary>
    /// <param name="fileStream">A <see cref="Stream"/> that has read access to a GPX file.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    public GPXFile(Stream fileStream)
    {
        ArgumentNullException.ThrowIfNull(fileStream, nameof(fileStream));

        if (!fileStream.CanRead)
            throw new ArgumentException("Stream must have read access.", nameof(fileStream));

        XmlSerializer serializer = new(typeof(GPX));
        gpx = (GPX)serializer.Deserialize(fileStream);
    }

    /// <summary>
    /// Gets the underlying data of the <see cref="GPXFile"/> in the form of a <see cref="GPX"/>.
    /// </summary>
    public GPX Data => gpx;
    private readonly GPX gpx;
}
