using Windows.Media.Core;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Legends;
using OxyPlot.Annotations;
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

        /// <summary>
        /// Gets the <see cref="SST.ProjectFile"/> that the <see cref="TelemetryView"/> manages.
        /// </summary>
        public ProjectFile ProjectFile { get; }

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
        /// <param name="project">The <see cref="SST.ProjectFile"/> to manage.</param>
        public TelemetryView(TelemetryFile file, ProjectFile project)
        {
            InitializeComponent();

            //Populate properties
            TelemetryFile = file;
            ProjectFile = project;

            //Add layers if none exist
            project.Layers ??= [.. map.Children.Where(i => i is MapTileLayer).Select(i => (i as MapTileLayer).TileSource.UriTemplate)];

            //Item1 is the timestamp
            //Item2 is the fork
            //Item3 is the shock
            var data = ExtractData(file);

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
            telemetryCSV = TrimDataToCSV(data);

#pragma warning disable CS0618 //Type or member is obsolete
            model.Axes[0].AxisChanged += (s, e) => ZoomFactorChanged?.Invoke(s, ZoomFactor);
#pragma warning restore CS0618 //Type or member is obsolete

            plot.Model = model;

            media.TransportControls.IsSkipBackwardEnabled =
            media.TransportControls.IsSkipBackwardButtonVisible =
            media.TransportControls.IsSkipForwardEnabled =
            media.TransportControls.IsSkipForwardButtonVisible = true;
        }

        private void View_Loaded(object sender, RoutedEventArgs args)
        {
            stopwatch.Stop();
            Loaded -= View_Loaded;

            timeTip.Title = $"Opened in {stopwatch.ElapsedMilliseconds:N0} ms.";
            timeTip.Focus(FocusState.Programmatic);
            timeTip.IsOpen = true;

            if (dialog is not null)
            {
                dialog.XamlRoot = XamlRoot;
                _ = dialog.ShowAsync();
        }
        }

        private void HideTimeTip_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => timeTip.IsOpen = false;

        private static (int, int, int)[] ExtractData(TelemetryFile file)
        {
            (int, int, int)[] values = new (int, int, int)[file.Count];

            for (int i = 0; i < file.Count; ++i)
            {
                (int f, int s) = file[i];
                values[i] = (i, f, s);
            }

            return values;
        }

        #region Airtime

        /// <summary>
        /// Gets or sets whether the graph contains airtime annotations.
        /// </summary>
        public bool AreAirtimesVisible
        {
            get => model.Annotations.Count > 0;
            set
            {
                if (AreAirtimesVisible == value)
                    return;
                else if (value)
                    foreach (var annot in airAnnots)
                        model.Annotations.Add(annot);
                else
                    foreach (var annot in airAnnots)
                        model.Annotations.Remove(annot);

                model.InvalidatePlot(true);
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

        #region Video

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
                ShowVideo(ProjectFile.VideoPath = file.Path);
                IsDirty = true;
            }
        }

        /// <summary>
        /// Request a video to be added to the <see cref="TelemetryView"/> using the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">A path to a video file.</param>
        public void RequestVideo(string path) => ShowVideo(path);

        private void ShowVideo(string path)
        {
            media.Source = MediaSource.CreateFromUri(new(path));

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

        #endregion

        #region Map

        private const double MapZoomPadding = 0.005;

        private readonly List<TrackPoint> points = [];

        /// <summary>
        /// Request a map to be added to the <see cref="TelemetryView"/>.
        /// </summary>
        public async void RequestMap()
        {
            FileOpenPicker picker = new()
            {
                FileTypeFilter = { ".gpx" },
                SuggestedStartLocation = PickerLocationId.Downloads
            };
            WinRT.Interop.InitializeWithWindow.Initialize(picker, (nint)XamlRoot.ContentIslandEnvironment.AppWindowId.Value);

            if (await picker.PickSingleFileAsync() is StorageFile file)
                ShowMap(file);
        }

        /// <summary>
        /// Request a map to be added to the <see cref="TelemetryView"/> using the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">A path to a GPX file.</param>
        public async void RequestMap(string path)
        {
            try
            {
                ShowMap(await StorageFile.GetFileFromPathAsync(path), false);
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex.Message);
            }
        }

        private async void ShowMap(StorageFile file, bool makeDirty = true)
        {
            //Update layout of view to add map
            mapContainer.Visibility = Visibility.Visible;
            Grid.SetColumnSpan(plotContainer, 1);

            if (mediaContainer.Visibility == Visibility.Visible)
                Grid.SetRowSpan(mediaContainer, 1);
            else
            {
                Grid.SetRow(mapContainer, 0);
                Grid.SetRowSpan(mapContainer, 2);
            }

            //Remove existing map lines (while keeping layers) and clear points list
            points.Clear();
            foreach (var child in map.Children.Where(i => i is MapPolyline))
                map.Children.Remove(child);

            //Extract GPX file data
            GPX gpx;
            try
            {
                using var stream = await file.OpenStreamForReadAsync();
                gpx = new GPXFile(stream).Data;
            }
            catch
            {
                ShowErrorDialog("Incorrect file contents. It may be corrupt or of a different format.");
                return;
            }

            //Update project GPX path
            ProjectFile.GPXPath = file.Path;
            IsDirty = makeDirty;

            //Draw lines to display route(s)
            double minLatitude = int.MaxValue,
                minLongitude = int.MaxValue,
                maxLatitude = int.MinValue,
                maxLongitude = int.MinValue;

            foreach (var track in gpx.Tracks)
                foreach (var segment in track.Segments)
                {
                    if (segment.Points.Length < 1)
                        continue;

                    TrackPoint prevPoint = segment.Points[0];
                    int threshold = 0,
                        hr = 0;

                    foreach (var point in segment.Points.Skip(1).ToArray())
                    {
                        if (minLatitude > point.Latitude) minLatitude = point.Latitude;
                        else if (maxLatitude < point.Latitude) maxLatitude = point.Latitude;

                        if (minLongitude > point.Longitude) minLongitude = point.Longitude;
                        else if (maxLongitude < point.Longitude) maxLongitude = point.Longitude;

                        hr += point.Extensions.GarminExtension.HeartRate ?? 0;
                        points.Add(point);

                        if (threshold > 5)
                        {
                            map.Children.Add(new MapPolyline
                            {
                                Locations = LocationCollection.OrthodromeLocations(
                                    new(prevPoint.Latitude, prevPoint.Longitude),
                                    new(point.Latitude, point.Longitude)),
                                StrokeThickness = 2,
                                Stroke = new SolidColorBrush((hr / 5.0 / 220) switch
                                {
                                    0 => Colors.Black,
                                    > 0.9 => Colors.Red,
                                    > 0.8 => Colors.Orange,
                                    > 0.7 => Colors.Yellow,
                                    > 0.6 => Colors.YellowGreen,
                                    _ => Colors.Green
                                })
                            });

                            threshold = hr = 0;
                            prevPoint = point;
                        }
                        else
                            ++threshold;
                    }
                }

            //Zoom map to show drawn paths
            map.ZoomToBounds(new(
                minLatitude - MapZoomPadding,
                minLongitude - MapZoomPadding,
                maxLatitude + MapZoomPadding,
                maxLongitude + MapZoomPadding));
        }

        /// <summary>
        /// Request for a <see cref="MapLayerEditor"/> hosted in a <see cref="ContentDialog"/> to be shown.
        /// </summary>
        public async void RequestMapLayerEditor()
        {
            MapLayerEditor editor = new()
            {
                Width = 410,
                BaseLayer = new((map.Children[0] as MapTileLayer).TileSource.UriTemplate),
                Layers = map.Children.Skip(1)
                                     .Where(i => i is MapTileLayer)
                                     .Select(i => new Uri((i as MapTileLayer).TileSource.UriTemplate))
            };
            ContentDialog dialog = new()
            {
                Content = editor,
                XamlRoot = XamlRoot,
                CloseButtonText = "Cancel",
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

                map.Children[0] = new MapTileLayer { TileSource = new() { UriTemplate = editor.BaseLayer.OriginalString } };

                foreach (var child in map.Children.Skip(1).ToArray())
                {
                    if (child is MapTileLayer layer)
                        map.Children.Remove(layer);
                    else
                        break;
                }

                foreach (var layer in editor.Layers.Reverse())
                    map.Children.Insert(1, new MapTileLayer { TileSource = new() { UriTemplate = layer.OriginalString } });

            ProjectFile.Layers = [editor.BaseLayer.OriginalString, .. editor.Layers.Select(i => i.OriginalString)];
            IsDirty = true;
        }

        /// <summary>
        /// Sets the layers of the map to <paramref name="layers"/>.
        /// </summary>
        /// <param name="layers">The URL templates of the specified map tile sources.</param>
        public void SetMapLayers(string[] layers)
        {
            foreach (var child in map.Children)
            {
                if (child is MapTileLayer layer)
                    map.Children.Remove(layer);
                else
                    break;
            }

            foreach (var layer in layers.Reverse())
                map.Children.Insert(0, new MapTileLayer { TileSource = new() { UriTemplate = layer } });
        }

        #endregion

        #region AI

        private readonly ObservableCollection<AIPrompt> prompts = [];

        private static readonly GeminiModel aiModel = new(
            ModelVariant.Gemini25FlashLitePreview0617,
            "You are the 'Suspension Assistant', who is an assistant for an application that views MTB and dirt bike suspension usage analytics. You were created by the Suspension app to assist with suspension metrics and analytics.");

        private readonly string telemetryCSV;

        private void AIBox_KeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (args.Key == Windows.System.VirtualKey.Enter)
                SendButton_Click(sender, args);
        }

        private void SendButton_Click(object sender, RoutedEventArgs args)
        {
            if (aiDataToggle.IsChecked == true)
            {
                AnalyzeData($"Use the prior CSV to answer and/or help with the following prompt: {aiBox.Text}", aiBox.Text);
                aiDataToggle.IsChecked = false;
            }
            else
                MakeAIRequest(aiBox.Text);

            aiBox.Text = string.Empty;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs args)
        {
            contentGrid.ColumnDefinitions.RemoveAt(2);
            aiPane.Visibility = Visibility.Collapsed;

            Focus(FocusState.Programmatic);
            prompts.Clear();
        }

        private void NewChatButton_Click(object sender, RoutedEventArgs args) => RequestAIChat(true);

        private void AnalyzeData(string prompt, string uiOverride) => MakeAIRequest(
            $"Consider the following CSV as a representation of a bike's suspension usage. Timestamp is measured in 1/{TelemetryFile.SampleRate} of a second. Fork and Shock are measured in fractions of a degree.\n{telemetryCSV}\n{prompt}",
            uiOverride);

        private async void MakeAIRequest(string prompt, string uiOverride = null)
        {
            AIPrompt[] requests = [.. prompts, new(Role.User, prompt)];
            prompts.Add(uiOverride is null ? requests[^1] : new(Role.User, uiOverride));

            if (await aiModel.TryMakeRequest(requests) is AIResponse response)
                prompts.Add(new(Role.Model, response.ToString()));
        }

        public void RequestRiderStyle() => AnalyzeData("Use the prior CSV to determine the style of the rider. Answer in a single word or phrase.", "What is my rider style?");
        private static string TrimDataToCSV((int, int, int)[] data)
        {
            var lines = data.Select(i => $"{i.Item1},{i.Item2},{i.Item3}").ToArray();

            double lineAverageLength = lines.Average(i => i.Length);
            double maxLines = 50000 / lineAverageLength;
            int skipEvery = (int)(lines.Length / maxLines);

            List<string> selectedLines = ["Timestamp,Fork,Shock"];
            for (int i = 0; i < lines.Length; i += skipEvery)
                selectedLines.Add(lines[i]);

            return string.Join("\n", selectedLines);
        }

        #endregion

        #region Saving

        /// <summary>
        /// Gets whether the <see cref="TelemetryView"/> needs to be saved.
        /// </summary>
        public bool IsDirty
        {
            get => (bool)GetValue(IsDirtyProperty);
            private set => SetValue(IsDirtyProperty, value);
        }
        public static DependencyProperty IsDirtyProperty { get; } =
            DependencyProperty.Register(nameof(IsDirty), typeof(bool), typeof(TelemetryView), new(false));

        /// <summary>
        /// Requests <see cref="ProjectFile"/> to be saved.
        /// </summary>
        public void RequestSave()
        {
            if (ProjectFile.FilePath is null)
                RequestSaveLocation();
            else
                Save(ProjectFile.FilePath);
        }

        /// <summary>
        /// Requests <see cref="ProjectFile"/> to be saved to a location picked by the user.
        /// </summary>
        public async void RequestSaveLocation()
        {
            FileSavePicker picker = new()
            {
                FileTypeChoices =
                {
                    { "SST Project", [".sstproj"] }
                },
                SuggestedFileName = ProjectFile.FilePath is null ? "New SST Project" : Path.GetFileNameWithoutExtension(ProjectFile.FilePath),
                SuggestedStartLocation = PickerLocationId.Downloads
            };
            WinRT.Interop.InitializeWithWindow.Initialize(picker, (nint)XamlRoot.ContentIslandEnvironment.AppWindowId.Value);

            if (await picker.PickSaveFileAsync() is StorageFile file)
                Save(ProjectFile.FilePath = file.Path);
        }

        private async void Save(string path)
        {
            await ProjectFile.Save(path);
            IsDirty = false;
            }

        #endregion

        #region Errors

        private ContentDialog dialog;

        private void ShowErrorDialog(string content)
        {
            dialog = new()
            {
                Title = "Error",
                Content = content,
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            if (XamlRoot is not null)
                _ = dialog.ShowAsync();
        }

        #endregion
    }

    public partial class AIPromptTemplateSelector : DataTemplateSelector
    {
        public DataTemplate UserTemplate { get; set; }

        public DataTemplate ModelTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is AIPrompt prompt)
                return prompt.Role == Role.User ? UserTemplate : ModelTemplate;
            else
                return base.SelectTemplateCore(item);
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
    }
}
