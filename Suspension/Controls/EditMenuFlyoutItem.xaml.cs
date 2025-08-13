namespace Suspension.Controls
{
    public sealed partial class EditMenuFlyoutItem : RadioMenuFlyoutItem
    {
        public EditMenuFlyoutItem()
        {
            InitializeComponent();
        }

        private void EditButton_Click(object sender, RoutedEventArgs args)
        {
            var presenter = ItemsControl.ItemsControlFromItemContainer(this) as MenuFlyoutPresenter;
            (presenter.Parent as Popup).IsOpen = false;

            EditClick?.Invoke(sender, args);
        }

        /// <summary>
        /// Occurs when the internal edit button is clicked.
        /// </summary>
        public event RoutedEventHandler EditClick;
    }
}
