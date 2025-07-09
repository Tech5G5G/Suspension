namespace Suspension.Controls
{
    /// <summary>
    /// Represents a control that allows the user to edit <see cref="MapLayerEditorItem"/>s.
    /// </summary>
    public sealed partial class MapLayerEditor : UserControl
    {
        /// <summary>
        /// Gets or sets the <see cref="Uri"/>s of the layers shown in the <see cref="MapLayerEditor"/>.
        /// </summary>
        public IEnumerable<Uri> Layers
        {
            get => uris.Select(i => i.Source);
            set
            {
                uris.Clear();
                foreach (var layer in value)
                {
                    MapLayerEditorItem item = new(layer);
                    item.RemoveRequested += (s, e) => uris.Remove(item);
                    uris.Add(item);
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Uri"/> of the base layer shown.
        /// </summary>
        public Uri BaseLayer
        {
            get => (Uri)GetValue(BaseLayerProperty);
            set => SetValue(BaseLayerProperty, value);
        }
        public static DependencyProperty BaseLayerProperty { get; } =
            DependencyProperty.Register(nameof(BaseLayer), typeof(Uri), typeof(MapLayerEditor), new(null));

        private readonly ObservableCollection<MapLayerEditorItem> uris = [];

        /// <summary>
        /// Creates a new instance of <see cref="MapLayerEditor"/>.
        /// </summary>
        public MapLayerEditor()
        {
            InitializeComponent();
        }

        private void BaseItem_SourceChanged(object sender, RoutedEventArgs args) => BaseLayer = baseItem.Source;

        private void AddButton_Click(object sender, RoutedEventArgs args)
        {
            addButton.Visibility = Visibility.Collapsed;
            addItem.Visibility = Visibility.Visible;
        }

        private void AddItem_SourceChanged(object sender, RoutedEventArgs args)
        {
            AddItem_SourceChangeCanceled(sender, args);

            uris.Insert(0, new(addItem.Source));
            addItem.Source = null;
        }

        private void AddItem_SourceChangeCanceled(object sender, RoutedEventArgs args)
        {
            addButton.Visibility = Visibility.Visible;
            addItem.Visibility = Visibility.Collapsed;

            addItem.IsEditing = true;
        }
    }
}
