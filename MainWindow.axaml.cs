using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using TaskPin.ViewModels;

namespace TaskPin;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly Border _windowSurface;
    private TextBox? _editingTextBox;

    public MainWindow() : this(new MainWindowViewModel(new Services.AppDataStore()))
    {
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Opacity = viewModel.Settings.Opacity;
        _windowSurface = this.FindControl<Border>("WindowSurface")!;

        Opened += (_, _) => ApplyPosition();
        Closing += (_, _) => _viewModel.SaveTaskEdits();
        AddHandler(PointerPressedEvent, Window_PointerPressed, RoutingStrategies.Tunnel);
    }

    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (_editingTextBox is not null)
        {
            var position = e.GetPosition(_editingTextBox);
            if (!new Rect(_editingTextBox.Bounds.Size).Contains(position))
            {
                _windowSurface.Focus();
            }
        }

        if (!IsInteractiveControl(e.Source as Control))
        {
            BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    private static bool IsInteractiveControl(Control? control)
    {
        for (var current = control; current is not null; current = current.Parent as Control)
        {
            if (current is TextBox or Button or CheckBox or ScrollBar)
            {
                return true;
            }
        }

        return false;
    }

    private void NewTask_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel.AddTaskCommand.CanExecute(null))
        {
            _viewModel.AddTaskCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void TaskTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        _editingTextBox = sender as TextBox;
    }

    private void TaskTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        _viewModel.SaveTaskEdits();
        if (ReferenceEquals(_editingTextBox, sender))
        {
            _editingTextBox = null;
        }
    }

    private async void OpenSettings_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_viewModel.Settings);
        var saved = await dialog.ShowDialog<bool>(this);
        if (!saved)
        {
            return;
        }

        Opacity = _viewModel.Settings.Opacity;
        _viewModel.SaveSettings();
        ApplyPosition();
    }

    private void Quit_Click(object? sender, RoutedEventArgs e) => Close();

    private void ApplyPosition()
    {
        var settings = _viewModel.Settings;
        var screen = Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var workArea = screen.WorkingArea;
        var windowSize = PixelSize.FromSize(Bounds.Size, screen.Scaling);
        var margin = (int)Math.Ceiling(16 * screen.Scaling);
        var x = settings.StartPosition.EndsWith("Right", StringComparison.Ordinal)
            ? workArea.Right - windowSize.Width - margin
            : workArea.X + margin;
        var y = settings.StartPosition.StartsWith("Bottom", StringComparison.Ordinal)
            ? workArea.Bottom - windowSize.Height - margin
            : workArea.Y + margin;

        Position = new PixelPoint(x, y);
    }
}