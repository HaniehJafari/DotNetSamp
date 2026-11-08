
using System.Text.Json;

namespace DotnetTaskRecoveryConsoleApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var tasks = new List<TaskItem>
        {
            new() { Id = "1", Name = "Task1" },
            new() { Id = "2", Name = "Task2" },
            new() { Id = "3", Name = "Task3" },
            new() { Id = "4", Name = "Task4" },
            new() { Id = "5", Name = "Task5" }
        };

        var repository = new TaskStateRepository("task-state.json");

        var manager = new TaskManager(tasks, repository);

        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await manager.RunTasksAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(
                "\nExecution cancelled. Previous completed tasks are preserved.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nApplication error: {ex.Message}");
        }
    }
}

// ==========================================
// TaskItem
// ==========================================

public class TaskItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

// ==========================================
// TaskState
// ==========================================

public class TaskState
{
    public string TaskId { get; set; } = string.Empty;

    public string TaskName { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    public DateTime LastUpdated { get; set; }
}

// ==========================================
// TaskStateRepository
// ==========================================

public class TaskStateRepository
{
    private readonly string _filePath;

    private readonly string _tempFilePath;

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };

    public TaskStateRepository(string filePath)
    {
        _filePath = Path.GetFullPath(filePath);

        _tempFilePath = _filePath + ".tmp";
    }

    // Load previously saved states.
    public async Task<List<TaskState>> LoadAsync(
        List<TaskItem> tasks)
    {
        if (!File.Exists(_filePath))
        {
            var initialStates = tasks.Select(task =>
                new TaskState
                {
                    TaskId = task.Id,
                    TaskName = task.Name,
                    IsCompleted = false,
                    LastUpdated = DateTime.UtcNow
                }).ToList();

            await SaveAsync(initialStates);

            return initialStates;
        }

        await using var stream = new FileStream(
            _filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            useAsync: true);

        var states =
            await JsonSerializer.DeserializeAsync<List<TaskState>>(
                stream,
                _options);

        if (states is null)
        {
            throw new InvalidDataException(
                "Task state file is invalid.");
        }

        ValidateStates(tasks, states);

        return states;
    }

    // Save states using a temporary file.
    public async Task SaveAsync(List<TaskState> states)
    {
        var directory = Path.GetDirectoryName(_filePath)!;

        Directory.CreateDirectory(directory);

        var json = JsonSerializer.SerializeToUtf8Bytes(
            states,
            _options);

        await using (var stream = new FileStream(
            _tempFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            useAsync: true))
        {
            await stream.WriteAsync(json);

            await stream.FlushAsync();

            stream.Flush(flushToDisk: true);
        }

        // Replace the original file.
        File.Move(
            _tempFilePath,
            _filePath,
            overwrite: true);
    }

    // Validate task definitions.
    private static void ValidateStates(
        List<TaskItem> tasks,
        List<TaskState> states)
    {
        if (tasks.Count != states.Count)
        {
            throw new InvalidDataException(
                "Task definitions have changed.");
        }

        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].Id != states[i].TaskId ||
                tasks[i].Name != states[i].TaskName)
            {
                throw new InvalidDataException(
                    $"Task definition mismatch at index {i}.");
            }
        }
    }
}

// ==========================================
// TaskManager
// ==========================================

public class TaskManager
{
    private readonly List<TaskItem> _tasks;

    private readonly TaskStateRepository _repository;

    private List<TaskState> _states = [];

    public TaskManager(
        List<TaskItem> tasks,
        TaskStateRepository repository)
    {
        _tasks = tasks;

        _repository = repository;
    }

    public async Task RunTasksAsync(
        CancellationToken cancellationToken = default)
    {
        // Step 1: Recover previous execution state.
        _states = await _repository.LoadAsync(_tasks);

        Console.WriteLine("Task execution started.\n");

        // Step 2: Execute tasks sequentially.
        for (int i = 0; i < _tasks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var task = _tasks[i];

            var state = _states[i];

            // Skip completed tasks.
            if (state.IsCompleted)
            {
                Console.WriteLine(
                    $"[SKIPPED] {task.Name} already completed.");

                continue;
            }

            Console.WriteLine($"[RUNNING] {task.Name}");

            try
            {
                // Execute current task.
                await ExecuteTaskAsync(
                    task,
                    cancellationToken);

                // Update task state.
                state.IsCompleted = true;

                state.LastUpdated = DateTime.UtcNow;

                // Persist immediately after successful execution.
                await _repository.SaveAsync(_states);

                Console.WriteLine(
                    $"[COMPLETED] {task.Name}\n");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FAILED] {task.Name}");

                Console.WriteLine(
                    $"Error: {ex.Message}");

                // Stop execution.
                throw;
            }
        }

        Console.WriteLine(
            "All tasks completed successfully.");
    }

    // Simulate task execution.
    private async Task ExecuteTaskAsync(
        TaskItem task,
        CancellationToken cancellationToken)
    {
        await Task.Delay(2000, cancellationToken);

        Console.WriteLine(
            $"Processing {task.Name}...");
    }
}
