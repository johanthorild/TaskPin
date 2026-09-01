using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TaskPin.Models;

namespace TaskPin;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow() : this(new AppSettings())
    {
    }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        OpacitySlider.Value = settings.Opacity;
        OpacityValue.Text = $"{settings.Opacity:P0}";
        OpacitySlider.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == nameof(OpacitySlider.Value))
            {
                OpacityValue.Text = $"{OpacitySlider.Value:P0}";
            }
        };

        PositionComboBox.SelectedItem = PositionComboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == settings.StartPosition);
    }

    private void Header_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        _settings.Opacity = OpacitySlider.Value;
        if (PositionComboBox.SelectedItem is ComboBoxItem selected)
        {
            _settings.StartPosition = selected.Tag?.ToString() ?? "TopRight";
        }

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}