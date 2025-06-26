using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Legends;

namespace Suspension.Views
{
    /// <summary>
    /// A page that shows information stored by a <see cref="TelemetryFile"/>.
    /// </summary>
    public sealed partial class TelemetryView : Page
    {
        /// <summary>
        /// Gets the <see cref="SST.TelemetryFile"/> used to create this <see cref="TelemetryView"/>.
        /// </summary>
        public TelemetryFile TelemetryFile { get; private set; }

        /// <summary>
        /// Gets the <see cref="PlotModel"/> used in this <see cref="TelemetryView"/>.
        /// </summary>
        public PlotModel Model => model;

        private readonly PlotModel model = new()
        {
            Legends =
            {
                new Legend { LegendPosition = LegendPosition.BottomRight }
            },
            IsLegendVisible = true
        };

        /// <summary>
        /// Creates a new instance of <see cref="TelemetryView"/>.
        /// </summary>
        /// <param name="file">The <see cref="SST.TelemetryFile"/> to display.</param>
        public TelemetryView(TelemetryFile file)
        {
            InitializeComponent();
            TelemetryFile = file;

            //Item1 is the timestamp
            //Item2 is the fork
            //Item3 is the shock
            (int, int, int)[] data = ExtractData();

            //Create graph line for fork
            model.Series.Add(new LineSeries()
            {
                Title = "Fork",
                MinimumSegmentLength = 20,
                Color = OxyColor.FromRgb(0x84, 0x43, 0x54),
                ItemsSource = data.Select(i => new DataPoint(i.Item1, i.Item2)).ToArray()
            });

            //Create graph line for shock
            model.Series.Add(new LineSeries
            {
                Title = "Shock",
                MinimumSegmentLength = 20,
                Color = OxyColor.FromRgb(0x37, 0xA9, 0xCF),
                ItemsSource = data.Select(i => new DataPoint(i.Item1, i.Item3)).ToArray()
            });

            plot.Model = model;
            model.DefaultXAxis.Title = "Time";
            model.DefaultYAxis.Title = "Travel";
        }

        private (int, int, int)[] ExtractData()
        {
            int end = TelemetryFile.Count;
            (int, int, int)[] values = new (int, int, int)[end--];

            for (int i = 0; i < end; ++i)
            {
                (int f, int s) = TelemetryFile[i];
                values[i] = (i, f, s);
            }

            return values;
        }
    }
}
