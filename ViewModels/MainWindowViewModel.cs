using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskPin.Models;
using TaskPin.Services;

namespace TaskPin.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly AppDataStore _store;

    public MainWindowViewModel(AppDataStore store)
    {
        _store = store;
        Settings = store.LoadSettings();

        foreach (var task in store.LoadTasks())
        {
            AddLoadedTask(task);
        }
    }

    public ObservableCollection<TaskItemViewModel> Tasks { get; } = [];

    public AppSettings Settings { get; }

    public bool IsEmpty => Tasks.Count == 0;

    public bool HasCompletedTasks => Tasks.Any(task => task.IsCompleted);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddTaskCommand))]
    private string _newTaskText = string.Empty;

    [RelayCommand(CanExecute = nameof(CanAddTask))]
    private void AddTask()
    {
        var text = NewTaskText.Trim();
        if (text.Length == 0)
        {
            return;
        }

        AddLoadedTask(new TaskRecord(Guid.NewGuid(), text, false));
        NewTaskText = string.Empty;
        SaveTasks();
        UpdateCollectionState();
    }

    private bool CanAddTask() => !string.IsNullOrWhiteSpace(NewTaskText);

    [RelayCommand(CanExecute = nameof(HasCompletedTasks))]
    private void ClearCompleted()
    {
        foreach (var task in Tasks.Where(task => task.IsCompleted).ToArray())
        {
            RemoveTask(task);
        }
    }

    public void SaveSettings() => _store.SaveSettings(Settings);

    public void SaveTaskEdits() => SaveTasks();

    public void MoveTask(TaskItemViewModel task, TaskItemViewModel target)
    {
        var oldIndex = Tasks.IndexOf(task);
        var newIndex = Tasks.IndexOf(target);
        if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex)
        {
            return;
        }

        Tasks.Move(oldIndex, newIndex);
        SaveTasks();
    }

    private void AddLoadedTask(TaskRecord task)
    {
        var item = new TaskItemViewModel(task, RemoveTask);
        item.PropertyChanged += OnTaskPropertyChanged;
        Tasks.Add(item);
    }

    private void RemoveTask(TaskItemViewModel task)
    {
        task.PropertyChanged -= OnTaskPropertyChanged;
        Tasks.Remove(task);
        SaveTasks();
        UpdateCollectionState();
    }

    private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TaskItemViewModel.IsCompleted))
        {
            SaveTasks();
            UpdateCollectionState();
        }
    }

    private void SaveTasks() => _store.SaveTasks(Tasks.Select(task => task.ToRecord()));

    private void UpdateCollectionState()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasCompletedTasks));
        ClearCompletedCommand.NotifyCanExecuteChanged();
    }
}