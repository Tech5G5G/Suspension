namespace Suspension.Controls
{
    /// <summary>
    /// Represents a control that edits a <see cref="Settings.Profiles.Profile"/>.
    /// </summary>
    public sealed partial class ProfileEditor : UserControl
    {
        /// <summary>
        /// Gets or sets the <see cref="Settings.Profiles.Profile"/> to edit.
        /// </summary>
        public Profile Profile
        {
            get => (Profile)GetValue(ProfileProperty);
            set => SetValue(ProfileProperty, value);
        }
        public static DependencyProperty ProfileProperty { get; } =
            DependencyProperty.Register(nameof(Profile), typeof(Profile), typeof(ProfileEditor), new(null));

        /// <summary>
        /// Gets the <see cref="Settings.Profiles.Profile"/> created by the user.
        /// </summary>
        /// <remarks>Returns <see langword="null"/> if <see cref="Profile"/> is not set.</remarks>
        public Profile NewProfile { get; private set; }

        private readonly List<FontIcon> fontIcons = [];

        private static readonly Int32ToColorConverter converter = new();

        /// <summary>
        /// Creates a new instance of <see cref="ProfileEditor"/>.
        /// </summary>
        public ProfileEditor()
        {
            InitializeComponent();

            icons.ItemsSource = Enum.GetValues<Symbol>().Select(i => ((int)i).ToString("X"))
                                                        .Select(i => Convert.ToChar(Convert.ToInt32(i, 16)))
                                                        .Select(i => i.ToString())
                                                        .ToArray();

            forkA.NumberFormatter = forkB.NumberFormatter =
            shockA.NumberFormatter = shockB.NumberFormatter = new UnitFormatter { Unit = "mm" };

            RegisterPropertyChangedCallback(ProfileProperty, (s, e) =>
            {
                //Clone profile
                NewProfile = (Profile)Profile.Clone();

                //Set color of icons
                bool enabled = (colorToggle.IsChecked = Profile.Color.HasValue) == true;
                SolidColorBrush brush = enabled ? new(converter.Convert(Profile.Color.Value)) : (SolidColorBrush)Resources["FontIconForegroundBrush"];

                SetFontIconsForeground(brush);

                picker.Color = brush.Color;
                picker.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

                //Set selected icon
                icons.SelectedIndex = Array.IndexOf((Array)icons.ItemsSource, icon.Glyph = Profile.Icon);

                //Set values of dimension NumberBoxes
                forkA.Value = Profile.ForkDimensions.SideA;
                forkB.Value = Profile.ForkDimensions.SideB;

                shockA.Value = Profile.ShockDimensions.SideA;
                shockB.Value = Profile.ShockDimensions.SideB;
            });
        }

        #region Icons

        private void ColorToggle_Click(object sender, RoutedEventArgs args)
        {
            bool enabled = colorToggle.IsChecked == true;
            picker.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            SetFontIconsForeground(enabled ? new(picker.Color) : (SolidColorBrush)Resources["FontIconForegroundBrush"]);
        }

        private void Picker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args) => SetFontIconsForeground(new(args.NewColor));

        private void FontIcon_Loaded(object sender, RoutedEventArgs args)
        {
            var icon = sender as FontIcon;
            fontIcons.Add(icon);
            icon.Foreground = colorToggle.IsChecked == true ? new(picker.Color) : (SolidColorBrush)Resources["FontIconForegroundBrush"];
        }

        private void SetFontIconsForeground(SolidColorBrush brush)
        {
            icon.Foreground = brush;
            NewProfile.Color = brush == (SolidColorBrush)Resources["FontIconForegroundBrush"] ? null : converter.ConvertBack(brush.Color);

            foreach (var icon in fontIcons)
                icon.Foreground = brush;
        }

        private void Icons_SelectionChanged(object sender, SelectionChangedEventArgs args) => NewProfile.Icon = icon.Glyph = (string)args.AddedItems[0];

        #endregion

        #region Sides

        private void Fork_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            double a = NewProfile.ForkDimensions.SideA = forkA.Value;
            double b = NewProfile.ForkDimensions.SideB = forkB.Value;
            double c = Math.Sqrt(a * a + b * b - 2 * a * b * Math.Cos(1));

            double y3 = (c * c - (b * b - a * a)) / (2 * c);
            double x3 = Math.Sqrt(a * a - y3 * y3);

            forkTriangle.Points =
            [
                new(),
                new(0, c),
                new(x3, y3)
            ];

            double stroke = c / 50;
            forkTriangle.StrokeThickness = double.IsNaN(stroke) ? 0 : stroke;

            double offset = c / (50 / 8);
            forkTriangle.Margin = new(double.IsNaN(stroke) ? 0 : offset, 0, 0, 0);
        }

        private void Shock_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            double a = NewProfile.ShockDimensions.SideA = shockA.Value;
            double b = NewProfile.ShockDimensions.SideB = shockB.Value;
            double c = Math.Sqrt(a * a + b * b - 2 * a * b * Math.Cos(1));

            double y3 = (c * c - (b * b - a * a)) / (2 * c);
            double x3 = Math.Sqrt(a * a - y3 * y3);

            shockTriangle.Points =
            [
                new(),
                new(0, c),
                new(x3, y3)
            ];

            double stroke = c / 50;
            shockTriangle.StrokeThickness = double.IsNaN(stroke) ? 0 : stroke;

            double offset = c / (50 / 8);
            shockTriangle.Margin = new(double.IsNaN(stroke) ? 0 : offset, 0, 0, 0);
        }

        private void ForkInfoButton_Click(object sender, RoutedEventArgs args) => forkInfoTip.IsOpen = !forkInfoTip.IsOpen;

        private void ShockInfoButton_Click(object sender, RoutedEventArgs args) => shockInfoTip.IsOpen = !shockInfoTip.IsOpen;

        #endregion

        #region Divisor

        private void Divisor_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) => NewProfile.AngleDivisor = args.NewValue;

        private void DivisorInfoButton_Click(object sender, RoutedEventArgs args) => divisorTip.IsOpen = !divisorTip.IsOpen;

        #endregion

        #region Other

        private void NameBox_TextChanged(object sender, TextChangedEventArgs args) => NewProfile.Name = (sender as TextBox).Text;

        private void DefaultCheck_Checked(object sender, RoutedEventArgs args) => NewProfile.IsDefault = (sender as CheckBox).IsChecked == true;

        #endregion
    }
}
