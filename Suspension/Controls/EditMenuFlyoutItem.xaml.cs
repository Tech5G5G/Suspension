namespace Suspension.Controls
{
    public sealed partial class EditMenuFlyoutItem : MenuFlyoutItem
    {
        /// <summary>
        /// Occurs when the internal edit button is clicked.
        /// </summary>
        public event RoutedEventHandler EditClick;

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set
            {
                SetValue(IsCheckedProperty, value);
                UpdateCheck();
            }
        }
        public static DependencyProperty IsCheckedProperty { get; } =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(EditMenuFlyoutItem), new(false));

        private FontIcon CheckGlyph;

        public EditMenuFlyoutItem()
        {
            InitializeComponent();
            Click += MenuFlyoutItem_Click;
        }

        protected override void OnApplyTemplate()
        {
            CheckGlyph = (FontIcon)GetTemplateChild(nameof(CheckGlyph));
            UpdateCheck();

            base.OnApplyTemplate();
        }

        private void EditButton_Click(object sender, RoutedEventArgs args)
        {
            var presenter = ItemsControl.ItemsControlFromItemContainer(this) as MenuFlyoutPresenter;
            (presenter.Parent as Popup).IsOpen = false;

            EditClick?.Invoke(sender, args);
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs args) => IsChecked = true;

        private void UpdateCheck()
        {
            if (CheckGlyph is not null)
                CheckGlyph.Opacity = IsChecked ? 1 : 0;

            if (!IsChecked)
                return;

            var presenter = ItemsControl.ItemsControlFromItemContainer(this) as MenuFlyoutPresenter;
            if (presenter is not null)
                foreach (var item in presenter.Items)
                {
                    if (item is EditMenuFlyoutItem edit && edit != this)
                        edit.IsChecked = false;
                }
        }
    }
}
