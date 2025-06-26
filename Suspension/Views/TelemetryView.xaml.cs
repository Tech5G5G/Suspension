using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Legends;
using Windows.Media.Core;

namespace Suspension.Views
{
    /// <summary>
    /// A page that shows information stored by a <see cref="TelemetryFile"/>.
    /// </summary>
    public sealed partial class TelemetryView : Page
    {
        /// <summary>
        /// Gets the <see cref="SST.TelemetryFile"/> used to create the <see cref="TelemetryView"/>.
        /// </summary>
        public TelemetryFile TelemetryFile { get; private set; }

        /// <summary>
        /// Gets the <see cref="PlotModel"/> used in the <see cref="TelemetryView"/>.
        /// </summary>
        public PlotModel Model => model;

        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

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
            model.DefaultXAxis.Unit = "s";

            model.DefaultYAxis.Title = "Travel";
            model.DefaultYAxis.Unit = "mm";
            model.DefaultYAxis.StartPosition = 1;
            model.DefaultYAxis.EndPosition = 0;

            Loaded += ShowLoadedTip;
        }

        private void ShowLoadedTip(object sender, RoutedEventArgs args)
        {
            stopwatch.Stop();
            Loaded -= ShowLoadedTip;

            timeTip.Title = $"Opened in {stopwatch.ElapsedMilliseconds} ms.";
            timeTip.IsOpen = true;
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

        //TODO: Complete implementation of request methods

        {
            FileOpenPicker picker = new()
            {
                FileTypeFilter =
                {
                    ".mp4",
                    ".wmv",
                    ".avi",
                    ".mov",
                    ".mkv"
                },
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            WinRT.Interop.InitializeWithWindow.Initialize(picker, (nint)XamlRoot.ContentIslandEnvironment.AppWindowId.Value);

            if (await picker.PickSingleFileAsync() is StorageFile file)
            {
                media.Source = MediaSource.CreateFromUri(new(file.Path));
                media.Visibility = Visibility.Visible;
                openMediaButton.Visibility = Visibility.Collapsed;
            }
        }
    }
}
