using System.Text.Json;

namespace ReliableTaskProcessor
{
   
    public enum TaskStatus
    {
        Pending, 
        Processing, 
        Completed, 
        Failed 
    }

    public class TaskItem
    {
        
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Description { get; set; }
        public TaskStatus Status { get; set; }
    }

    public class TaskManager
    {
        private readonly string _filePath = "tasks.json";
        private readonly string _tempFilePath = "tasks.json.tmp";
        private List<TaskItem> _tasks;

        public TaskManager()
        {
            LoadTasks();
        }

        private void LoadTasks()
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _tasks = JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
            }
            else
            {
                _tasks = new List<TaskItem>();
            }

           
            bool needsSave = false;
            foreach (var task in _tasks.Where(t => t.Status == TaskStatus.Processing))
            {
                task.Status = TaskStatus.Pending;
                needsSave = true;
            }

            if (needsSave) Save();
        }

        public void AddTask(string description)
        {
            _tasks.Add(new TaskItem
            {
                Description = description,
                Status = TaskStatus.Pending
            });
            Save();
        }

        public List<TaskItem> GetPendingTasks()
        {
            return _tasks.Where(t => t.Status == TaskStatus.Pending).ToList();
        }

        public void UpdateStatus(string id, TaskStatus newStatus)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                task.Status = newStatus;
                Save();
            }
        }

       
        private void Save()
        {
            var json = JsonSerializer.Serialize(_tasks, new JsonSerializerOptions { WriteIndented = true });

            
            File.WriteAllText(_tempFilePath, json);

           
            File.Move(_tempFilePath, _filePath, overwrite: true);
        }
    }

    public class TaskProcessor
    {
        private readonly TaskManager _taskManager;

        public TaskProcessor(TaskManager taskManager)
        {
            _taskManager = taskManager;
        }

        public void ProcessTasks()
        {
            var tasks = _taskManager.GetPendingTasks();

            foreach (var task in tasks)
            {
                try
                {
                   
                    _taskManager.UpdateStatus(task.Id, TaskStatus.Processing);

                    Console.WriteLine($"Processing Task {task.Id}: {task.Description}");

                    if (task.Description.Contains("fail", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception("Simulated failure");
                    }

                   
                    _taskManager.UpdateStatus(task.Id, TaskStatus.Completed);
                    Console.WriteLine($"Task {task.Id} completed successfully.");
                }
                catch (Exception ex)
                {
                    
                    Console.WriteLine($"Error processing task {task.Id}: {ex.Message}");
                    _taskManager.UpdateStatus(task.Id, TaskStatus.Failed);
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var manager = new TaskManager();

            if (args.Length > 0 && args[0] == "add")
            {
                var description = string.Join(' ', args, 1, args.Length - 1);
                manager.AddTask(description);
                Console.WriteLine("Task added.");
                return;
            }

            var processor = new TaskProcessor(manager);
            processor.ProcessTasks();
        }
    }
}