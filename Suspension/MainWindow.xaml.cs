using System.Globalization;
using Windows.Graphics;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Globalization.NumberFormatting;
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
            LoadProfiles();
            SetupWelcome();
        }

        #region Setup

        private void LoadSettings()
        {
            airtimeToggle.IsChecked = SettingValues.Airtimes;
            SettingValues.Airtimes.ValueChanged += (s, e) =>
            {
                airtimeToggle.IsChecked = e;

                if (mainView.Content is TelemetryView view)
                    view.AreAirtimesVisible = e;
            };

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
                welcomeView.Visibility = Visibility.Collapsed;

                tabs.CanDragTabs = tabs.CanReorderTabs =
                viewMenu.IsEnabled = editMenu.IsEnabled = assistantButton.IsEnabled =
                saveButton.IsEnabled = saveAsButton.IsEnabled = saveAllButton.IsEnabled = true;

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
            if (args.AddedItems.Count < 1 ||
                args.AddedItems[0] is not TelemetryItem item)
                return;

                mainView.Content = item.TelemetryView;
                item.TelemetryView.AreAirtimesVisible = airtimeToggle.IsChecked;

                sizeText.Text = $"{item.TelemetryFile.Size / 1024:N0} KB";
                pointsText.Text = $"{item.TelemetryFile.Count * 2:N0} points";

            var items = profilesMenu.Items.Where(i => i is EditMenuFlyoutItem);
            if (items.Cast<EditMenuFlyoutItem>().FirstOrDefault(i => i.IsChecked) is EditMenuFlyoutItem checkedItem)
                checkedItem.IsChecked = false;
            if (items.FirstOrDefault(i => (i.DataContext as Profile).Id == item.TelemetryView.Profile.Id) is EditMenuFlyoutItem flyoutItem)
                flyoutItem.IsChecked = true;

                if (args.RemovedItems.Count > 0 &&
                    args.RemovedItems[0] is TelemetryItem prevItem)
                    DeregisterZooming(prevItem.TelemetryView);

                RegisterZooming(item.TelemetryView);
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
            if (File.Exists(recentsFile))
            {
                var destination = await Task.Run(() => JumpList.JumpList.LoadAutoJumplist(recentsFile));

                foreach (var item in destination.DestListEntries)
                {
                    FileInfo info = null;
                    try
                    {
                        info = new(item.Path);
                        if (!info.Exists)
                            continue;
                    }
                    catch
                    {
                        continue;
                    }

                    RecentItem recent = new(info.Name, info.DirectoryName, item.LastModified.ToLocalTime().ToString("f"), item.Path);
                    recents.Add(recent);

                    MenuFlyoutItem flyout = new() { Text = info.Name };
                    flyout.Click += (s, e) => TryAddRecentItem(recent);
                    recentsMenu.Items.Add(flyout);
                }

                recentsMenu.IsEnabled = true;
            }

            recentsRing.Visibility = Visibility.Collapsed;
        }

        private void Recents_ItemClick(object sender, ItemClickEventArgs args) => TryAddRecentItem(args.ClickedItem as RecentItem);

        private async void TryAddRecentItem(RecentItem item)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(item.FullName);
                TryAddTelemetryFile(file);
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex.Message);
            }
        }

        private async void OpenButton_Click(object sender, RoutedEventArgs args)
        {
            FileOpenPicker picker = new()
            {
                FileTypeFilter = { ".sst", ".sstproj" },
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            WinRT.Interop.InitializeWithWindow.Initialize(picker, this.GetWindowHandle());

            if (await picker.PickSingleFileAsync() is StorageFile file)
                TryAddTelemetryFile(file);
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs args)
        {
            FolderPicker picker = new() { SuggestedStartLocation = PickerLocationId.ComputerFolder };
            WinRT.Interop.InitializeWithWindow.Initialize(picker, this.GetWindowHandle());

            if (await picker.PickSingleFolderAsync() is StorageFolder folder)
                foreach (var file in await folder.GetFilesAsync())
                {
                    if (file.FileType.Equals(".sst", StringComparison.OrdinalIgnoreCase) ||
                        file.FileType.Equals(".sstproj", StringComparison.OrdinalIgnoreCase))
                        TryAddTelemetryFile(file);
                }
        }

        private void Content_DragEnter(object sender, DragEventArgs args)
        {
            if (args.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems) &&
                args.AllowedOperations.HasFlag(DataPackageOperation.Copy))
                args.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void Content_Drop(object sender, DragEventArgs args)
        {
            if (await args.DataView.GetStorageItemsAsync() is IReadOnlyList<IStorageItem> items)
                foreach (var item in items)
                {
                    if (item is StorageFile file &&
                        (file.FileType.Equals(".sst", StringComparison.OrdinalIgnoreCase) ||
                        file.FileType.Equals(".sstproj", StringComparison.OrdinalIgnoreCase)))
                        TryAddTelemetryFile(file);
                }
        }

        public void LoadTelemetryFromArguments(AppActivationArguments args)
        {
            if (args.Data is IFileActivatedEventArgs fileArgs && fileArgs.Files[0] is IStorageFile file)
                this.DispatcherQueue.TryEnqueue(() => TryAddTelemetryFile(file));
        }

        private async void TryAddTelemetryFile(IStorageFile file)
        {
            try
            {
                using var stream = await file.OpenStreamForReadAsync();

                var profiles = Profile.GetProfilesAsync().Result;
                var defaultProfile = profiles.FirstOrDefault(i => i.IsDefault, profiles.First());

                if (file.FileType.Equals(".sst", StringComparison.OrdinalIgnoreCase))
                {
                    TelemetryFile telemetry = new(stream);
                    this.telemetry.Add(new(file.Name, telemetry, new(
                        telemetry,
                        new() { SSTPath = file.Path, ProfileId = defaultProfile.Id },
                        defaultProfile)));
                }
                else if (file.FileType.Equals(".sstproj", StringComparison.OrdinalIgnoreCase))
                {
                    ProjectFile project = new(stream, file.Path);

                    if (!new FileInfo(project.SSTPath).Exists)
                    {
                        ShowErrorDialog("SST project file does not exist.");
                        return;
                    }

                    StorageFile telemetry = await StorageFile.GetFileFromPathAsync(project.SSTPath);
                    TelemetryFile telemetryFile = new(await telemetry.OpenStreamForReadAsync());
                    TelemetryView view = new(
                        telemetryFile,
                        project,
                        profiles.FirstOrDefault(i => i.Id == project.ProfileId, defaultProfile));
                    
                    if (project.VideoPath is not null)
                        view.RequestVideo(project.VideoPath);

                    if (project.GPXPath is not null)
                        view.RequestMap(project.GPXPath);

                    if (project.Layers.Length > 0)
                        view.SetMapLayers(project.Layers);

                    view.VideoOffset = project.VideoOffset;
                    this.telemetry.Add(new(Path.GetFileNameWithoutExtension(file.Path), telemetryFile, view));
                }
                else
                    ShowErrorDialog("Incorrect file type. Accepted types are .sst and .sstproj files.");
            }
            catch
            {
                ShowErrorDialog("Incorrect file contents. It may be corrupt or of a different format.");
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
        }

        private void EditLayersButton_Click(object sender, RoutedEventArgs args) => (mainView.Content as TelemetryView)?.RequestMapLayerEditor();

        private void AirtimeToggle_Click(object sender, RoutedEventArgs args)
        {
            if (mainView.Content is TelemetryView view)
                view.AreAirtimesVisible = SettingValues.Airtimes.Value = (sender as ToggleMenuFlyoutItem).IsChecked;
        }

        private void ToggleStatusBar_Click(object sender, RoutedEventArgs args) =>
            ShowStatusBar(SettingValues.StatusBar.Value = (sender as ToggleMenuFlyoutItem).IsChecked);

        private void ResetZoomButton_Click(object sender, RoutedEventArgs args)
        {
            if (mainView.Content is TelemetryView view)
                view.ZoomFactor = 1 + ZoomOffset;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs args)
        {
            if (mainView.Content is TelemetryView view && (view.IsDirty || view.ProjectFile.FilePath is null))
                view.RequestSave();
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs args) => (mainView.Content as TelemetryView)?.RequestSaveLocation();
        
        private void SaveAllButton_Click(object sender, RoutedEventArgs args)
        {
            foreach (var item in telemetry)
                if (item.TelemetryView.IsDirty)
                    item.TelemetryView.RequestSave();
        }

        private void NewWindowButton_Click(object sender, RoutedEventArgs args) => App.CreateWindow();

        private void ExitButton_Click(object sender, RoutedEventArgs args) => Environment.Exit(0);

        private void ShowStatusBar(bool show)
        {
            statusBar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            mainView.Margin = show ? default : new(0, 0, 0, 8);
        }

        #endregion

        #region Profiles

        private async void LoadProfiles()
        {
            Guid selectedId = (profilesMenu.Items.FirstOrDefault(i => i is EditMenuFlyoutItem { IsChecked: true })?.DataContext as Profile)?.Id ?? Guid.Empty;
            for (int i = profilesMenu.Items.Count - 2; i > 0; --i)
                profilesMenu.Items.RemoveAt(0);

            if (await Profile.GetProfilesAsync() is not Profile[] profiles || profiles.Length < 1)
            {
                await Profile.SaveProfile(new()
                {
                    Name = "Default",
                    Id = Guid.NewGuid(),
                    Icon = "\uE13D",
                    Color = null,
                    IsDefault = true,
                    AngleDivisor = 1,
                    ForkDimensions = new(),
                    ShockDimensions = new()
                });
                profiles = await Profile.GetProfilesAsync();
            }

            Int32ToColorConverter converter = new();
            foreach (var profile in profiles.Reverse())
            {
                FontIcon icon = new() { Glyph = profile.Icon };
                if (profile.Color is not null)
                    icon.Foreground = new SolidColorBrush(converter.Convert(profile.Color.Value));

                EditMenuFlyoutItem item = new()
                {
                    Icon = icon,
                    Text = profile.Name,
                    DataContext = profile
                };
                item.EditClick += async (s, e) =>
                {
                    ProfileEditor editor = new() { Profile = profile };
                    ContentDialog dialog = new()
                    {
                        Title = "Edit profile",
                        Content = editor,
                        PrimaryButtonText = "Save",
                        CloseButtonText = "Cancel",
                        XamlRoot = Content.XamlRoot,
                        DefaultButton = ContentDialogButton.Primary,
                    };

                    if (Profile.GetProfilesAsync().Result.Length > 1)
                        dialog.SecondaryButtonText = "Remove";

                    switch (await dialog.ShowAsync())
                    {
                        case ContentDialogResult.Primary:
                            if (editor.NewProfile.IsDefault &&
                                Profile.GetProfilesAsync().Result.FirstOrDefault(i => i.IsDefault) is Profile oldProfile)
                            {
                                oldProfile.IsDefault = false;
                                await Profile.SaveProfile(oldProfile);
                            }
                            else if (!editor.NewProfile.IsDefault &&
                                Profile.GetProfilesAsync().Result.FirstOrDefault(i => i.IsDefault) is null)
                                editor.NewProfile.IsDefault = true;

                            await Profile.SaveProfile(editor.NewProfile);
                            LoadProfiles();
                            break;
                        case ContentDialogResult.Secondary:
                            await Profile.RemoveProfile(profile.Id);
                            LoadProfiles();
                            break;
                    }
                };
                item.Click += (s, e) =>
                {
                    if (mainView.Content is TelemetryView view)
                        view.Profile = profile;
                };

                profilesMenu.Items.Insert(0, item);
            }

            if (profilesMenu.Items.FirstOrDefault(i => i.DataContext is Profile profile && profile.Id == selectedId) is EditMenuFlyoutItem selected)
                selected.IsChecked = true;
            else if (profilesMenu.Items.FirstOrDefault(i => i.DataContext is Profile { IsDefault: true }) is EditMenuFlyoutItem item)
                item.IsChecked = true;
            else if (profilesMenu.Items[0] is EditMenuFlyoutItem first)
                first.IsChecked = true;
        }

        private async void NewProfile_Click(object sender, RoutedEventArgs args)
        {
            ProfileEditor editor = new()
            {
                Profile = new()
                {
                    Name = "New profile",
                    Id = Guid.NewGuid(),
                    Icon = "\uE13D",
                    Color = null,
                    IsDefault = false,
                    AngleDivisor = 1,
                    ForkDimensions = new(),
                    ShockDimensions = new()
                }
            };
            ContentDialog dialog = new()
            {
                Title = "Create profile",
                Content = editor,
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                XamlRoot = Content.XamlRoot,
                DefaultButton = ContentDialogButton.Primary,
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                if (editor.NewProfile.IsDefault &&
                    Profile.GetProfilesAsync().Result.FirstOrDefault(i => i.IsDefault) is Profile oldProfile)
                {
                    oldProfile.IsDefault = false;
                    await Profile.SaveProfile(oldProfile);
                }
                else if (!editor.NewProfile.IsDefault &&
                    Profile.GetProfilesAsync().Result.FirstOrDefault(i => i.IsDefault) is null)
                    editor.NewProfile.IsDefault = true;

                await Profile.SaveProfile(editor.NewProfile);
                LoadProfiles();
            }
        }

        #endregion

        #region Assistant

        private void NewChatButton_Click(object sender, RoutedEventArgs args) => (mainView.Content as TelemetryView)?.RequestAIChat(true);

        private void RiderStyleButton_Click(object sender, RoutedEventArgs args)
        {
            if (mainView.Content is TelemetryView view)
            {
                view.RequestAIChat(false);
                view.RequestRiderStyle();
            }
        }

        private void TuningAdviceButton_Click(object sender, RoutedEventArgs args)
        {
            if (mainView.Content is TelemetryView view)
            {
                view.RequestAIChat(false);
                view.RequestTuningAdvice();
            }
        }

        private void MaintenanceButton_Click(object sender, RoutedEventArgs args)
        {
            if (mainView.Content is TelemetryView view)
            {
                view.RequestAIChat(false);
                view.RequestMaintenanceAlerts();
            }
        }

        private async void KeyButton_Click(object sender, RoutedEventArgs args)
        {
            PasswordBox box = new()
            {
                Width = 385,
                PlaceholderText = "Gemini API key",
                Password = GeminiKeyVault.Key
            };

            ContentDialog dialog = new()
            {
                Title = "Edit API key",
                Content = box,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                IsPrimaryButtonEnabled = false,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            bool canBeEmpty = !string.IsNullOrWhiteSpace(box.Password);
            box.PasswordChanged += (s, e) =>
                dialog.IsPrimaryButtonEnabled = box.Password.Length == 39 || (box.Password.Length == 0 && canBeEmpty);

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                GeminiKeyVault.Key = box.Password;
        }

        #endregion

        #region Zooming

        private const double ZoomOffset = 0.02;

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
                view.ZoomFactor = args.NewValue + ZoomOffset;
        }

        private void ZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs args)
        {
            if (mainView.Content is TelemetryView view)
                view.ZoomFactor = args.NewValue + ZoomOffset;
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

    public partial class UnitFormatter : INumberFormatter, INumberFormatter2, INumberParser
    {
        public string Unit { get; set; } = string.Empty;

        private string GetFormattedUnit() => $"{(string.IsNullOrWhiteSpace(Unit) ? string.Empty : " ")}{Unit}";

        public string Format(long value) => $"{value}{GetFormattedUnit()}";

        public string Format(ulong value) => $"{value}{GetFormattedUnit()}";

        public string Format(double value) => $"{value}{GetFormattedUnit()}";

        public string FormatDouble(double value) => Format(value);

        public string FormatInt(long value) => Format(value);

        public string FormatUInt(ulong value) => Format(value);

        public double? ParseDouble(string text) => double.TryParse(text.Replace($" {Unit}", null), out double value) ? value : null;

        public long? ParseInt(string text) => long.TryParse(text.Replace($" {Unit}", null), out long value) ? value : null;

        public ulong? ParseUInt(string text) => ulong.TryParse(text.Replace($" {Unit}", null), out ulong value) ? value : null;
    }

    public partial class DoubleToPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            value is double num ? $"{Math.Round(Math.Round(num, 2) * 100)}%" : value;

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            value is string str ? double.TryParse(str.Replace("%", null), out double num) ? num / 100 : value : value;
    }

    public partial class Int32ToColorConverter : IValueConverter
    {
        public Color Convert(int color) => (Color)Convert(color, null, null, null);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int color)
            {
                string colorStr = color.ToString("X6");
                return Color.FromArgb(
                    0xFF,
                    byte.Parse(colorStr[..2], NumberStyles.HexNumber),
                    byte.Parse(colorStr[2..4], NumberStyles.HexNumber),
                    byte.Parse(colorStr[4..6], NumberStyles.HexNumber));
            }
            else
                return value;
        }

        public int ConvertBack(Color color) => (int)ConvertBack(color, null, null, null);

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Color color)
            {
                string colorStr = $"{color.R:X2}{color.G:X2}{color.B:X2}";
                return int.Parse(colorStr, NumberStyles.HexNumber);
            }
            else
                return value;
        }
    }

    #endregion
}
