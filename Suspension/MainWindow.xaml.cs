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
                statsContainer.Visibility = Visibility.Visible;

                addVideoButton.IsEnabled = addMapButton.IsEnabled = true;
            }
        }

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

        private void CloseTabButton_Click(object sender, RoutedEventArgs args) => RemoveTabItem(telemetry[tabs.SelectedIndex]);

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
            var stream = await TelemetryFile.StreamFromPath(item.FullName);
            TryAddTelemetryFile(stream, item.FileName);
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
                ContentDialog dialog = new()
                {
                    Title = "Error",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = Content.XamlRoot
                };
                _ = dialog.ShowAsync();
            }
        }

        public void AddTelemetryItem(TelemetryItem telemetryItem) => telemetry.Add(telemetryItem);

        #endregion

        #region MenuBar

        private void AddVideoButton_Click(object sender, RoutedEventArgs args) => (mainView.Content as TelemetryView)?.RequestVideo();

        private void AddMapButton_Click(object sender, RoutedEventArgs args) => (mainView.Content as TelemetryView)?.RequestMap();

        private void NewWindowButton_Click(object sender, RoutedEventArgs args) => App.CreateWindow();

        private void ExitButton_Click(object sender, RoutedEventArgs args) => Environment.Exit(0);

        #endregion
    }

    #region Records

    public record RecentItem(string FileName, string FilePath, string LastModified, string FullName);

    public record TelemetryItem(string FileName, TelemetryFile TelemetryFile, TelemetryView TelemetryView);

    #endregion
}
