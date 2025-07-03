using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Legends;
using OxyPlot.Annotations;
using Windows.Media.Core;
using MapControl;

namespace Suspension.Views
{
    /// <summary>
    /// A page that shows information stored by a <see cref="TelemetryFile"/>.
    /// </summary>
    public sealed partial class TelemetryView : Page
    {
        /// <summary>
        /// Gets or sets the zoom factor of the graph.
        /// </summary>
        public double ZoomFactor
        {
            get => TelemetryFile.Count / TelemetryFile.SampleRate / (model.Axes[0].ActualMaximum - model.Axes[0].ActualMinimum);
            set
            {
                plot.ResetAllAxes();
                plot.ZoomAllAxes(value);
                plot.InvalidatePlot();
            }
        }

        /// <summary>
        /// Occurs when <see cref="ZoomFactor"/> is changed.
        /// </summary>
        public event EventHandler<double> ZoomFactorChanged;

        /// <summary>
        /// Gets the <see cref="SST.TelemetryFile"/> used to create the <see cref="TelemetryView"/>.
        /// </summary>
        public TelemetryFile TelemetryFile { get; }

        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        private readonly PlotModel model = new()
        {
            Legends = { new Legend { LegendPosition = LegendPosition.BottomRight } },
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
            model.Series.Add(new LineSeries
            {
                Title = "Fork",
                Color = OxyColor.FromRgb(0x84, 0x43, 0x54),
                ItemsSource = data.Select(i => new DataPoint((double)i.Item1 / file.SampleRate, i.Item2)).ToArray()
            });

            //Create graph line for shock
            model.Series.Add(new LineSeries
            {
                Title = "Shock",
                Color = OxyColor.FromRgb(0x37, 0xA9, 0xCF),
                ItemsSource = data.Select(i => new DataPoint((double)i.Item1 / file.SampleRate, i.Item3)).ToArray()
            });

            //Create X-Axis
            model.Axes.Add(new TimeSpanAxis
            {
                Title = "Time",
                Unit = "s",
                Position = AxisPosition.Bottom
            });

            //Create Y-Axis
            model.Axes.Add(new LinearAxis
            {
                Title = "Travel",
                StartPosition = 1,
                EndPosition = 0,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                Position = AxisPosition.Left
            });

            DetermineAirtimes(data);

#pragma warning disable CS0618 //Type or member is obsolete
            model.Axes[0].AxisChanged += (s, e) => ZoomFactorChanged?.Invoke(s, ZoomFactor);
#pragma warning restore CS0618 //Type or member is obsolete

            plot.Model = model;
            Loaded += ShowLoadedTip;

            media.TransportControls.IsSkipBackwardEnabled =
            media.TransportControls.IsSkipBackwardButtonVisible =
            media.TransportControls.IsSkipForwardEnabled =
            media.TransportControls.IsSkipForwardButtonVisible = true;
        }

        private void ShowLoadedTip(object sender, RoutedEventArgs args)
        {
            stopwatch.Stop();
            Loaded -= ShowLoadedTip;

            timeTip.Title = $"Opened in {stopwatch.ElapsedMilliseconds:N0} ms.";
            timeTip.Focus(FocusState.Programmatic);
            timeTip.IsOpen = true;
        }

        private void HideTimeTip_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => timeTip.IsOpen = false;

        private (int, int, int)[] ExtractData()
        {
            int end = TelemetryFile.Count - 1;
            (int, int, int)[] values = new (int, int, int)[end];

            for (int i = 0; i < end; ++i)
            {
                (int f, int s) = TelemetryFile[i];
                values[i] = (i, f, s);
            }

            return values;
        }

        #region Airtime

        private const float AirtimeTravelThreshold = 3, // (mm) maximum travel to consider stroke an airtime
                            AirtimeDurationThreshold = 0.20f, // (s) minimum duration to consider stroke an airtime
                            AirtimeVelocityThreshold = 500, // (mm/s) minimum velocity after stroke to consider it an airtime
                            AirtimeOverlapThreshold = 0.5f, // f&r airtime candidates must overlap at least this amount to be an airtime
                            AirtimeTravelMeanThresholdRatio = 0.04f; // stroke f&r mean travel must be below max*this to be an airtime

        private void DetermineAirtimes()
        {
            get => model.Annotations.Count > 0;
            set
            {
                (int t, int f, int s) = data[i];

                if (f < AirtimeTravelThreshold && s < AirtimeTravelThreshold)
                    values[i] = (double)t / TelemetryFile.SampleRate;
            }
        }
        private readonly List<RectangleAnnotation> airAnnots = [];

        private const float AirtimeTravelThreshold = 3,
                            AirtimeDurationThreshold = 0.20f;

        private void DetermineAirtimes((int, int, int)[] data)
            {
            AirtimeItem currentItem = new() { Min = double.NegativeInfinity };

            foreach (var item in data.Where(i => i.Item2 < AirtimeTravelThreshold && i.Item3 < AirtimeTravelThreshold)
                                     .Select(i => (double)i.Item1 / TelemetryFile.SampleRate)
                                     .Select(i =>
                {
                                         if (currentItem.Min == double.NegativeInfinity)
                                             currentItem.Min = i;
                                         else if (Math.Ceiling(currentItem.Max * TelemetryFile.SampleRate) + 1 == Math.Ceiling(i * TelemetryFile.SampleRate))
                                             currentItem.Max = i;
                                         else
                                             currentItem = new() { Min = i, Max = i };

                                         return currentItem;
                                     })
                                     .ToArray()
                                     .Distinct()
                                     .Where(i => i.Max - i.Min is double x &&
                                                 x < double.PositiveInfinity &&
                                                 x >= AirtimeDurationThreshold))
            {
                RectangleAnnotation annot = new()
                {
                    Fill = OxyColor.FromArgb(0x4E, 0xFF, 0xDE, 0x2B),
                    Text = $"{item.Max - item.Min:0.#}s air time",
                    TextRotation = 270,
                    MaximumX = item.Max,
                    MinimumX = item.Min,
                };
                model.Annotations.Add(annot);
                airAnnots.Add(annot);
            }
        }

        private record AirtimeItem
        {
            public double Max { get; set; }
            public double Min { get; set; }
        }

        #endregion

        /// <summary>
        /// Request a video to be added to the <see cref="TelemetryView"/>.
        /// </summary>
        public async void RequestVideo()
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

                mediaContainer.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(plotContainer, 1);

                if (mapContainer.Visibility == Visibility.Visible)
                {
                    Grid.SetRow(mapContainer, 1);
                    Grid.SetRowSpan(mapContainer, 1);
                }
                else
                    Grid.SetRowSpan(mediaContainer, 2);
            }
        }

        /// <summary>
        /// Request a map to be added to the <see cref="TelemetryView"/>.
        /// </summary>
        public /*async*/ void RequestMap()
        {
            FileOpenPicker picker = new()
            {
                FileTypeFilter = { /*File type for mapping*/ },
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            WinRT.Interop.InitializeWithWindow.Initialize(picker, (nint)XamlRoot.ContentIslandEnvironment.AppWindowId.Value);

            //if (await picker.PickSingleFileAsync() is StorageFile file)
            //{
            //Add map layer
            mapContainer.Visibility = Visibility.Visible;
            Grid.SetColumnSpan(plotContainer, 1);

            if (mediaContainer.Visibility == Visibility.Visible)
                Grid.SetRowSpan(mediaContainer, 1);
            else
            {
                Grid.SetRow(mapContainer, 0);
                Grid.SetRowSpan(mapContainer, 2);
            }
            //}
        }

        /// <summary>
        /// Sets the base layer of the map to the specified <paramref name="uri"/>.
        /// </summary>
        /// <param name="uri">The URL template of the specified map tile source.</param>
        public void SetMapBaseLayer(string uri) => map.Children[0] = new MapTileLayer { TileSource = new() { UriTemplate = uri } };

        /// <summary>
        /// Adds a layer to the map using the specified <paramref name="uri"/>.
        /// </summary>
        /// <param name="uri">The URL template of the specified map tile source.</param>
        public void AddMapLayer(string uri) => map.Children.Add(new MapTileLayer { TileSource = new() { UriTemplate = uri } });
    }
}
