using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskPin.Models;

namespace TaskPin.ViewModels;

public partial class TaskItemViewModel : ObservableObject
{
    private readonly Action<TaskItemViewModel> _delete;

    public TaskItemViewModel(TaskRecord task, Action<TaskItemViewModel> delete)
    {
        Id = task.Id;
        _text = task.Text;
        _isCompleted = task.IsCompleted;
        _delete = delete;
    }

    public Guid Id { get; }

    [ObservableProperty]
    private string _text;

    [ObservableProperty]
    private bool _isCompleted;

    [RelayCommand]
    private void Delete() => _delete(this);

    public TaskRecord ToRecord() => new(Id, Text, IsCompleted);
}