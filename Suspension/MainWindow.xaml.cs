using Windows.System;
using Windows.Graphics;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Globalization.NumberFormatting;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;

namespace Suspension
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly WindowManager manager;
        private readonly InputNonClientPointerSource input;

        private readonly ObservableCollection<TelemetryItem> telemetry = [];
        private readonly ObservableCollection<RecentItem> recents = [];

        private static readonly string recentsFile =
            $"C:/Users/{Environment.UserName}/AppData/Roaming/Microsoft/Windows/Recent/AutomaticDestinations/936bee2f9d868b02.automaticDestinations-ms";

        public MainWindow()
        {
            InitializeComponent();
            AppWindow.SetIcon("Assets/Suspension.ico");

            manager = WindowManager.Get(this);
            manager.MinWidth = 650;
            manager.MinHeight = 400;

            input = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
            SetupTitleBar();

            SizeChanged += (s, e) =>
            {
                var isRestored = (AppWindow.Presenter as OverlappedPresenter).State == OverlappedPresenterState.Restored;
                tabs.Padding = isRestored ? new(0, 8, 0, 0) : new();

                var icon = maximizeButton.Content as FontIcon;
                maximizeToolTip.Content = isRestored ? "Maximize" : "Restore Down";
                icon.Glyph = isRestored ? "\uE922" : "\uE923";
            };

            zoomInButton.KeyboardAccelerators.Add(new() { Modifiers = VirtualKeyModifiers.Control, Key = (VirtualKey)0xBB });
            zoomOutButton.KeyboardAccelerators.Add(new() { Modifiers = VirtualKeyModifiers.Control, Key = (VirtualKey)0xBD });

            telemetry.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems[0] is TelemetryItem item)
                    DeregisterZooming(item.TelemetryView);
            };

            LoadSettings();
            SetupWelcome();
        }

        #region Setup

        private void LoadSettings()
        {
            SettingValues.BaseMapLayer.ValueChanged += (s, e) => (mainView.Content as TelemetryView)?.SetMapBaseLayer(e);

            statusBarToggle.IsChecked = SettingValues.StatusBar;
            SettingValues.StatusBar.ValueChanged += (s, e) =>
            {
                statusBarToggle.IsChecked = e;

                if (welcomeView.Visibility == Visibility.Collapsed)
                    ShowStatusBar(e);
            };
        }

        private void SetupWelcome()
        {
            if (telemetry.Count > 0)
                HideWelcome();
            else
            {
                void CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
                {
                    if (args.Action == NotifyCollectionChangedAction.Add)
                    {
                        HideWelcome();
                        telemetry.CollectionChanged -= CollectionChanged;
                    }
                }

                telemetry.CollectionChanged += CollectionChanged;
                LoadRecents();
            }

            void HideWelcome()
            {
                tabs.TabItemsSource = telemetry;
                tabs.CanDragTabs = tabs.CanReorderTabs = true;

                welcomeView.Visibility = Visibility.Collapsed;

                statusBarToggle.IsEnabled =
                addVideoButton.IsEnabled = addMapButton.IsEnabled =
                zoomInButton.IsEnabled = zoomOutButton.IsEnabled = resetZoomButton.IsEnabled = true;

                ShowStatusBar(statusBarToggle.IsChecked);
            }
        }

        #endregion

        #region Title bar

        private void SetupTitleBar()
        {
            SetTitleBar(dragRegion);
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;

            if (Environment.OSVersion.Version.Build >= 22000)
            {
                maximizeToolTip.Visibility = Visibility.Collapsed;
                var content = Content as FrameworkElement;

                Activated += (s, e) => SetMaximizeButtonBounds();
                input.ExitedMoveSize += (s, e) => SetMaximizeButtonBounds();
                content.SizeChanged += (s, e) => SetMaximizeButtonBounds();
                content.Loaded += (s, e) => SetMaximizeButtonBounds();
            }
        }

        private void SetMaximizeButtonBounds()
        {
            if (!AppWindow.IsVisible || Content.XamlRoot is not XamlRoot root ||
                !maximizeButton.TransformToVisual(null).TryTransform(new(), out Point transform))
                return;

            double scale = root.RasterizationScale;
            int Scale(double value) => (int)Math.Round(value * scale);

            RectInt32 maximizeRect = new(
                Scale(transform.X),
                Scale(transform.Y),
                Scale(maximizeButton.ActualWidth),
                Scale(maximizeButton.ActualHeight));
            input.SetRegionRects(NonClientRegionKind.Maximize, [maximizeRect]);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs args) => this.Minimize();

        private void CloseButton_Click(object sender, RoutedEventArgs args) => Close();

        private void MaximizeButton_Click(object sender, RoutedEventArgs args)
        {
            if ((AppWindow.Presenter as OverlappedPresenter).State == OverlappedPresenterState.Restored)
                this.Maximize();
            else
                this.Restore();
        }

        #endregion

        #region TabView

        private const string SourceWindowKey = "SourceWindow",
                             TelemetryItemKey = "TelemetryItem";

        private void Tabs_TabItemsChanged(TabView sender, IVectorChangedEventArgs args)
        {
            if (args.CollectionChange == CollectionChange.ItemInserted && telemetry.Count > 0)
                tabs.SelectedIndex = telemetry.Count - 1;
        }

        private void TabViewItem_Loaded(object sender, RoutedEventArgs args)
        {
            var data = (sender as FrameworkElement).DataContext;
            if (tabs.SelectedItem != data)
                tabs.SelectedItem = data;
        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (args.AddedItems.Count > 0 &&
                args.AddedItems[0] is TelemetryItem item)
            {
                mainView.Content = item.TelemetryView;

                sizeText.Text = $"{item.TelemetryFile.Size / 1024:N0} KB";
                pointsText.Text = $"{item.TelemetryFile.Count * 2:N0} points";

                if (args.RemovedItems.Count > 0 &&
                    args.RemovedItems[0] is TelemetryItem prevItem)
                    DeregisterZooming(prevItem.TelemetryView);

                RegisterZooming(item.TelemetryView);
            }
        }

        private void Tabs_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
        {
            if (telemetry.Count > 1 && args.Item is TelemetryItem item)
            {
                App.CreateWindow().AddTelemetryItem(item);
                RemoveTabItem(item);

                if ((Page)mainView.Content == item.TelemetryView)
                    mainView.Content = null;
            }
        }

        private void Tabs_TabStripDragOver(object sender, DragEventArgs args)
        {
            if (args.DataView.Properties.TryGetValue(TelemetryItemKey, out object obj) && !telemetry.Contains(obj))
                args.AcceptedOperation = DataPackageOperation.Move;
        }

        private void Tabs_TabStripDrop(object sender, DragEventArgs args)
        {
            if (args.DataView.Properties.TryGetValue(SourceWindowKey, out object win) &&
                args.DataView.Properties.TryGetValue(TelemetryItemKey, out object tel) &&
                win is MainWindow window &&
                tel is TelemetryItem item)
            {
                if ((TelemetryView)window.mainView.Content == item.TelemetryView)
                    window.mainView.Content = null;

                AddTelemetryItem(item);
                window.RemoveTabItem(item);
            }
        }

        private void Tabs_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            args.Data.Properties.Add(SourceWindowKey, this);
            args.Data.Properties.Add(TelemetryItemKey, args.Item);
            args.Data.RequestedOperation = DataPackageOperation.Move;
        }

        private void Tabs_AddTabButtonClick(TabView sender, object args) => OpenButton_Click(sender, new());

        private void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args) => RemoveTabItem(args.Item as TelemetryItem);

        private void CloseTabButton_Click(object sender, RoutedEventArgs args) =>
            RemoveTabItem(welcomeView.Visibility == Visibility.Visible ? null : telemetry[tabs.SelectedIndex]);

        private void RemoveTabItem(TelemetryItem item)
        {
            telemetry.Remove(item);
            if (telemetry.Count == 0)
                Close();
        }

        #endregion

        #region Telemetry

        private async void LoadRecents()
        {
            if (!File.Exists(recentsFile))
                return;

            var destination = await Task.Run(() => JumpList.JumpList.LoadAutoJumplist(recentsFile));

            foreach (var item in destination.DestListEntries)
            {
                FileInfo info = null;
                try
                {
                    info = new(item.Path);
                }
                catch
                {
                    continue;
                }

                recents.Add(new(info.Name, info.DirectoryName, item.LastModified.ToLocalTime().ToString("f"), item.Path));
            }

            recentsRing.Visibility = Visibility.Collapsed;
        }

        private async void Recents_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as RecentItem;
            try
            {
                TryAddTelemetryFile(await TelemetryFile.StreamFromPath(item.FullName), item.FileName);
            }
            catch (ArgumentException ex)
            {
                ShowErrorDialog(ex.Message);
            }
        }

        public void LoadTelemetryFromArguments(AppActivationArguments args)
        {
            if (args.Data is IFileActivatedEventArgs fileArgs && fileArgs.Files[0] is IStorageFile file)
                this.DispatcherQueue.TryEnqueue(async () => TryAddTelemetryFile(await file.OpenStreamForReadAsync(), file.Name));
        }

        private async void OpenButton_Click(object sender, RoutedEventArgs args)
        {
            FileOpenPicker picker = new()
            {
                FileTypeFilter = { ".sst" },
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            WinRT.Interop.InitializeWithWindow.Initialize(picker, this.GetWindowHandle());

            if (await picker.PickSingleFileAsync() is StorageFile file)
                TryAddTelemetryFile(await file.OpenStreamForReadAsync(), file.Name);
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs args)
        {
            FolderPicker picker = new() { SuggestedStartLocation = PickerLocationId.ComputerFolder };
            WinRT.Interop.InitializeWithWindow.Initialize(picker, this.GetWindowHandle());

            if (await picker.PickSingleFolderAsync() is StorageFolder folder)
            {
                foreach (var file in await folder.GetFilesAsync())
                {
                    if (file.FileType.Equals(".sst", StringComparison.OrdinalIgnoreCase))
                        TryAddTelemetryFile(await file.OpenStreamForReadAsync(), file.Name);
                }
            }
        }

        private void TryAddTelemetryFile(Stream stream, string name)
        {
            try
            {
                TelemetryFile file = new(stream);
                telemetry.Add(new(name, file, new(file)));
            }
            catch (InvalidDataException ex)
            {
                ShowErrorDialog(ex.Message);
            }
        }

        private void ShowErrorDialog(string content)
        {
            ContentDialog dialog = new()
            {
                Title = "Error",
                Content = content,
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot
            };
            _ = dialog.ShowAsync();
        }

        public void AddTelemetryItem(TelemetryItem telemetryItem) => telemetry.Add(telemetryItem);

        #endregion

        #region MenuBar

        private void AddVideoButton_Click(object sender, RoutedEventArgs args) => (mainView.Content as TelemetryView)?.RequestVideo();

        private void AddMapButton_Click(object sender, RoutedEventArgs args)
        {
            var view = mainView.Content as TelemetryView;
            view?.RequestMap();
            view?.SetMapBaseLayer(SettingValues.BaseMapLayer);
        }

        private async void SetMapBaseLayerButton_Click(object sender, RoutedEventArgs args)
            {
            if (await RequestStringAsync("Set map base layer", "Map tile URL") is string str)
            {
                (mainView.Content as TelemetryView)?.SetMapBaseLayer(str);
                SettingValues.BaseMapLayer.Value = str;
            }
        }

        private async void AddMapLayerButton_Click(object sender, RoutedEventArgs args)
        {
            if (await RequestStringAsync("Add map layer", "Map tile URL") is string str)
                (mainView.Content as TelemetryView)?.AddMapLayer(str);
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs args)
        {
            if (mainView.Content is TelemetryView view)
                view.ZoomFactor = 1.02;
        }

        private void NewWindowButton_Click(object sender, RoutedEventArgs args) => App.CreateWindow();

        private void ExitButton_Click(object sender, RoutedEventArgs args) => Environment.Exit(0);

        private void ShowStatusBar(bool show)
        {
            statusBar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            mainView.Margin = show ? default : new(0, 0, 0, 8);
        }

        private async Task<string> RequestStringAsync(string title, string placeholder)
        {
            TextBox box = new() { PlaceholderText = placeholder };
            ContentDialog dialog = new()
            {
                Title = title,
                Content = box,
                PrimaryButtonText = "OK",
                IsPrimaryButtonEnabled = false,
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot
            };
            box.TextChanged += (s, e) => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(box.Text);

            return await dialog.ShowAsync() == ContentDialogResult.Primary ? box.Text : null;
        }

        #endregion

        #region Zooming

        private void RegisterZooming(TelemetryView view)
        {
            zoomText.Value = zoomSlider.Value = view.ZoomFactor;
            view.ZoomFactorChanged += View_ZoomFactorChanged;
        }

        private void DeregisterZooming(TelemetryView view) => view.ZoomFactorChanged -= View_ZoomFactorChanged;

        private void DecreaseZoomButton_Click(object sender, RoutedEventArgs args)
        {
            if (mainView.Content is TelemetryView view)
                view.ZoomFactor -= 1;
        }

        private void IncreaseZoomButton_Click(object sender, RoutedEventArgs args)
        {
            if (mainView.Content is TelemetryView view)
                view.ZoomFactor += 1;
        }

        private void ZoomText_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (mainView.Content is TelemetryView view)
                view.ZoomFactor = args.NewValue + 0.02;
        }

        private void ZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs args)
        {
            if (mainView.Content is TelemetryView view)
                view.ZoomFactor = args.NewValue + 0.02;
        }

        private void View_ZoomFactorChanged(object sender, double args)
        {
            zoomText.ValueChanged -= ZoomText_ValueChanged;
            zoomSlider.ValueChanged -= ZoomSlider_ValueChanged;

            zoomText.Value = zoomSlider.Value = args;

            zoomText.ValueChanged += ZoomText_ValueChanged;
            zoomSlider.ValueChanged += ZoomSlider_ValueChanged;
        }

        #endregion
    }

    #region Types

    public record RecentItem(string FileName, string FilePath, string LastModified, string FullName);

    public record TelemetryItem(string FileName, TelemetryFile TelemetryFile, TelemetryView TelemetryView);

    public partial class PercentFormatter : INumberFormatter, INumberFormatter2, INumberParser
    {
        public string Format(long value) => $"{value}%";

        public string Format(ulong value) => $"{value}%";

        public string Format(double value) => $"{Math.Round(Math.Round(value, 2) * 100)}%";

        public string FormatDouble(double value) => Format(value);

        public string FormatInt(long value) => Format(value);

        public string FormatUInt(ulong value) => Format(value);

        public double? ParseDouble(string text) => double.TryParse(text.Replace("%", null), out double value) ? value / 100 : null;

        public long? ParseInt(string text) => long.TryParse(text.Replace("%", null), out long value) ? value : null;

        public ulong? ParseUInt(string text) => ulong.TryParse(text.Replace("%", null), out ulong value) ? value : null;
    }

    public partial class DoubleToPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            value is double num ? $"{Math.Round(Math.Round(num, 2) * 100)}%" : value;

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            value is string str ? double.TryParse(str.Replace("%", null), out double num) ? num / 100 : value : value;
    }

    #endregion
}
