namespace Suspension.Controls
{
    public sealed partial class MapLayerEditorItem : UserControl
    {
        /// <summary>
        /// Gets or sets the <see cref="Uri"/> source of the <see cref="MapLayerEditorItem"/>/
        /// </summary>
        public Uri Source
        {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
        public static DependencyProperty SourceProperty { get; } =
            DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(MapLayerEditorItem), new(null));

        /// <summary>
        /// Gets or sets whether the <see cref="MapLayerEditorItem"/> is being edited by the user.
        /// </summary>
        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }
        public static DependencyProperty IsEditingProperty { get; } =
            DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(MapLayerEditorItem), new(false));

        /// <summary>
        /// Gets or sets the <see cref="Visibility"/> of the remove button.
        /// </summary>
        public Visibility RemoveButtonVisibility
        {
            get => (Visibility)GetValue(RemoveButtonVisibilityProperty);
            set => SetValue(RemoveButtonVisibilityProperty, value);
        }
        public static DependencyProperty RemoveButtonVisibilityProperty { get; } =
            DependencyProperty.Register(nameof(RemoveButtonVisibility), typeof(bool), typeof(MapLayerEditorItem), new(Visibility.Visible));

        /// <summary>
        /// Occurs when the <see cref="Source"/> property is updated by the user.
        /// </summary>
        public event RoutedEventHandler SourceChanged;

        /// <summary>
        /// Occurs when the <see cref="IsEditing"/> property is set to <see langword="false"/> and the <see cref="Source"/> property isn't changed.
        /// </summary>
        public event RoutedEventHandler SourceChangeCanceled;

        /// <summary>
        /// Occurs when the <see cref="MapLayerEditorItem"/> is requested to be removed by the user.
        /// </summary>
        public event RoutedEventHandler RemoveRequested;

        /// <summary>
        /// Creates a new instance of <see cref="MapLayerEditorItem"/>.
        /// </summary>
        public MapLayerEditorItem()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates a new instance of <see cref="MapLayerEditorItem"/>, setting <see cref="Source"/> to <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The <see cref="Source"/> of the <see cref="MapLayerEditorItem"/>.</param>
        public MapLayerEditorItem(Uri source) : this() => Source = source;

        private void EditButton_Click(object sender, RoutedEventArgs args) => IsEditing = true;

        private void RemoveButton_Click(object sender, RoutedEventArgs args) => RemoveRequested?.Invoke(this, args);

        private void SourceBox_TextChanged(object sender, TextChangedEventArgs args) =>
            acceptButton.IsEnabled = !string.IsNullOrWhiteSpace((sender as TextBox).Text);

        private void AcceptButton_Click(object sender, RoutedEventArgs args)
        {
            string text = sourceBox.Text;
            string uri = text.Contains("https://") || text.Contains("http://") ? text : "https://" + text;

            if (!uri.Contains("{x}", StringComparison.InvariantCultureIgnoreCase) ||
                !uri.Contains("{y}", StringComparison.InvariantCultureIgnoreCase) ||
                !uri.Contains("{z}", StringComparison.InvariantCultureIgnoreCase))
                ShowTip("The specified URL should contain the following format items: {x} {y} {z}");
            else
            {
                try
                {
                    Source = new(uri);
                }
                catch
                {
                    ShowTip("The specified URL is not valid.");
                    return;
                }

                IsEditing = false;
                SourceChanged?.Invoke(this, args);
            }

            void ShowTip(string message)
            {
                acceptTip.Subtitle = message;
                acceptTip.IsOpen = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs args)
        {
            sourceBox.Text = Source?.ToString();
            IsEditing = false;
            SourceChangeCanceled?.Invoke(this, args);
        }
    }
}
