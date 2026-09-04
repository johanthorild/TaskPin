using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using TaskPin.ViewModels;

namespace TaskPin;

public partial class MainWindow : Window
{
    private const string TaskDragFormat = "application/x-taskpin-task";
    private readonly MainWindowViewModel _viewModel;
    private readonly Border _windowSurface;
    private readonly Canvas _dragPreviewLayer;
    private readonly Border _taskDragPreview;
    private readonly TextBlock _taskDragPreviewText;
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
        RestoreSavedSize();
        _windowSurface = this.FindControl<Border>("WindowSurface")!;
        _dragPreviewLayer = this.FindControl<Canvas>("DragPreviewLayer")!;
        _taskDragPreview = this.FindControl<Border>("TaskDragPreview")!;
        _taskDragPreviewText = this.FindControl<TextBlock>("TaskDragPreviewText")!;
        AddHandler(DragDrop.DragOverEvent, Task_DragOver);
        AddHandler(DragDrop.DropEvent, Task_Drop);

        Opened += (_, _) => ApplyPosition();
    Closing += (_, _) => SaveWindowState();
        AddHandler(PointerPressedEvent, Window_PointerPressed, RoutingStrategies.Tunnel);
    }

    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ContextMenu?.Close();

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
            if (current is TextBox or Button or CheckBox or ScrollBar ||
                current.Classes.Contains("taskDragHandle") ||
                current.Classes.Contains("resizeGrip"))
            {
                return true;
            }
        }

        return false;
    }

    private async void TaskDragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: TaskItemViewModel task } ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var data = new DataObject();
        data.Set(TaskDragFormat, task.Id.ToString());
        _taskDragPreviewText.Text = task.Text;
        _dragPreviewLayer.IsVisible = true;

        try
        {
            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        }
        finally
        {
            _dragPreviewLayer.IsVisible = false;
        }
    }

    private void Task_DragOver(object? sender, DragEventArgs e)
    {
        var canMove = e.Data.Contains(TaskDragFormat) && FindTask(e.Source) is not null;
        e.DragEffects = canMove ? DragDropEffects.Move : DragDropEffects.None;
        if (canMove)
        {
            var position = e.GetPosition(_dragPreviewLayer);
            var maxLeft = Math.Max(8, _dragPreviewLayer.Bounds.Width - _taskDragPreview.Bounds.Width - 8);
            var maxTop = Math.Max(8, _dragPreviewLayer.Bounds.Height - _taskDragPreview.Bounds.Height - 8);
            Canvas.SetLeft(_taskDragPreview, Math.Clamp(position.X + 14, 8, maxLeft));
            Canvas.SetTop(_taskDragPreview, Math.Clamp(position.Y + 14, 8, maxTop));
        }

        e.Handled = true;
    }

    private void Task_Drop(object? sender, DragEventArgs e)
    {
        if (FindTask(e.Source) is not { } target ||
            !Guid.TryParse(e.Data.Get(TaskDragFormat)?.ToString(), out var taskId))
        {
            return;
        }

        var task = _viewModel.Tasks.FirstOrDefault(item => item.Id == taskId);
        if (task is not null)
        {
            _viewModel.MoveTask(task, target);
        }

        e.Handled = true;
    }

    private static TaskItemViewModel? FindTask(object? source)
    {
        for (var current = source as Control; current is not null; current = current.Parent as Control)
        {
            if (current.DataContext is TaskItemViewModel task)
            {
                return task;
            }
        }

        return null;
    }

    private void ResizeGrip_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { Tag: string edgeName } &&
            Enum.TryParse<WindowEdge>(edgeName, out var edge) &&
            e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginResizeDrag(edge, e);
            e.Handled = true;
        }
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

    private void Minimize_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Quit_Click(object? sender, RoutedEventArgs e) => Close();

    private void RestoreSavedSize()
    {
        var settings = _viewModel.Settings;
        if (settings.WindowWidth is { } width)
        {
            Width = Math.Clamp(width, MinWidth, MaxWidth);
        }

        if (settings.WindowHeight is { } height)
        {
            Height = Math.Clamp(height, MinHeight, MaxHeight);
        }
    }

    private void SaveWindowState()
    {
        _viewModel.Settings.WindowWidth = Bounds.Width;
        _viewModel.Settings.WindowHeight = Bounds.Height;
        _viewModel.SaveSettings();
        _viewModel.SaveTaskEdits();
    }

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