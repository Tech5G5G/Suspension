using SkiaSharp;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinUI;
using LiveChartsCore.SkiaSharpView.Painting;

using OxyPlot;
using OxyPlot.Series;

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
        /// Gets the <see cref="CartesianChart"/> used in this <see cref="TelemetryView"/>.
        /// </summary>
        public CartesianChart Chart => chart;

        private readonly ObservableCollection<ISeries> series = [];

        private readonly ICartesianAxis[] xAxes = [new Axis()];
        private readonly ICartesianAxis[] yAxes = [new Axis()];


        private PlotModel model = new()
        {
            Title = "Example",
            Series =
            {
                new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)")
            }
        };

        /// <summary>
        /// Creates a new instance of <see cref="TelemetryView"/>.
        /// </summary>
        /// <param name="file">The <see cref="TelemetryFile"/> to display.</param>
        public TelemetryView(TelemetryFile file)
        {
            InitializeComponent();
            TelemetryFile = file;

            chart.EasingFunction = null;
            chart.LegendTextPaint = new SolidColorPaint(SKColors.White);

            chart.CacheMode = new BitmapCache();

            //Item1 is the timestamp
            //Item2 is the fork
            //Item3 is the shock
            (int, int, int)[] data = ExtractData();

            //Create graph line for fork
            series.Add(new LineSeries<ObservablePoint>
            {
                Name = "Fork",
                Values = [.. data.Select(i => new ObservablePoint(i.Item1, i.Item2))],
                Stroke = new SolidColorPaint(new(0x84, 0x43, 0x54)),
                Fill = new SolidColorPaint(new(0x84, 0x43, 0x54, 0x32)),
                GeometryStroke = null,
                GeometryFill = null
            });

            //Create graph line for shock
            series.Add(new LineSeries<ObservablePoint>
            {
                Name = "Shock",
                Values = [.. data.Select(i => new ObservablePoint(i.Item1, i.Item3))],
                Stroke = new SolidColorPaint(new(0x37, 0xA9, 0xCF)),
                Fill = new SolidColorPaint(new(0x37, 0xA9, 0xCF, 0x32)),
                GeometryStroke = null,
                GeometryFill = null
            });
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
