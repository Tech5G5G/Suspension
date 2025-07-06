using System.Xml;
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

#region Types

[XmlRoot(ElementName = "gpx", Namespace = "http://www.topografix.com/GPX/1/1")]
public class GPX
{
    [XmlElement("trk")]
    public Track[] Tracks { get; set; }

    [XmlElement("metadata")]
    public Metadata Metadata { get; set; }

    [XmlAttribute("creator")]
    public string Creator { get; set; }

    [XmlAttribute("version")]
    public string Version { get; set; }
}

public class Track
{
    [XmlElement("name")]
    public string Name { get; set; }

    [XmlElement("type")]
    public string Type { get; set; }

    [XmlElement("trkseg")]
    public TrackSegment[] Segments { get; set; }

    public override string ToString() => Name;
}

public class TrackSegment
{
    [XmlElement("trkpt")]
    public TrackPoint[] Points { get; set; }
}

public class TrackPoint
{
    [XmlAttribute("lat")]
    public double Latitude { get; set; }

    [XmlAttribute("lon")]
    public double Longitude { get; set; }

    [XmlElement("ele")]
    public double Elevation { get; set; }

    [XmlElement("time")]
    public DateTime Time { get; set; }

    [XmlElement("extensions")]
    public TrackPointExtensions Extensions { get; set; }

    public override string ToString() => $"{Latitude}, {Longitude}";
}

public class TrackPointExtensions
{
    [XmlElement("TrackPointExtension", Namespace = "http://www.garmin.com/xmlschemas/TrackPointExtension/v1")]
    public GarminTrackPointExtension GarminExtension { get; set; }
}

public class GarminTrackPointExtension
{
    /// <summary>
    /// Gets or sets the air temperature in celsius.
    /// </summary>
    [XmlElement("atemp")]
    public int? AirTemperature { get; set; }

    /// <summary>
    /// Gets or sets the water temperature in celsius.
    /// </summary>
    [XmlElement("wtemp")]
    public int? WaterTemperature { get; set; }

    /// <summary>
    /// Gets or sets depth in meters.
    /// </summary>
    [XmlElement("depth")]
    public double? Depth { get; set; }

    /// <summary>
    /// Gets or sets the heart rate in BPM.
    /// </summary>
    [XmlElement("hr")]
    public int? HeartRate { get; set; }

    /// <summary>
    /// Gets or sets the cadence in RPM.
    /// </summary>
    [XmlElement("cad")]
    public int? Cadence { get; set; }
}

public class Metadata
{
    [XmlElement("link")]
    public Link Link { get; set; }

    [XmlElement("time")]
    public DateTime Time { get; set; }
}

public class Link
{
    [XmlElement("text")]
    public string Text { get; set; }

    [XmlAttribute("href")]
    public string URL { get; set; }

    public override string ToString() => URL;
}

#endregion
