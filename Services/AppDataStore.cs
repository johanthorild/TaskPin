using System.Text.Json;
using TaskPin.Models;

namespace TaskPin.Services;

public sealed class AppDataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _directoryPath;
    private readonly string _tasksPath;
    private readonly string _settingsPath;

    public AppDataStore()
    {
        _directoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskPin");
        _tasksPath = Path.Combine(_directoryPath, "tasks.json");
        _settingsPath = Path.Combine(_directoryPath, "settings.json");
    }

    public IReadOnlyList<TaskRecord> LoadTasks() => Load(_tasksPath, new List<TaskRecord>());

    public AppSettings LoadSettings() => Load(_settingsPath, new AppSettings());

    public void SaveTasks(IEnumerable<TaskRecord> tasks) => Save(_tasksPath, tasks);

    public void SaveSettings(AppSettings settings) => Save(_settingsPath, settings);

    private static T Load<T>(string path, T fallback)
    {
        try
        {
            if (!File.Exists(path))
            {
                return fallback;
            }

            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
        catch (IOException)
        {
            return fallback;
        }
    }

    private void Save<T>(string path, T value)
    {
        try
        {
            Directory.CreateDirectory(_directoryPath);
            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(value, JsonOptions));
            File.Move(temporaryPath, path, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}