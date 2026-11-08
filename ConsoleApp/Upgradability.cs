
using System.Text.Json;
using System.Text.Json.Serialization;

#region Configuration

const string DataFile = "users.json";
const string TempFile = "users.json.tmp";
const int CurrentVersion = 3;

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNameCaseInsensitive = true
};

#endregion

#region Versioned Models

// Version 1
public class UserV1
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// Version 2
public class UserV2
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

// Version 3
public class UserV3
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DatabaseV1
{
    public int Version { get; set; } = 1;
    public List<UserV1> Users { get; set; } = [];
}

public class DatabaseV2
{
    public int Version { get; set; } = 2;
    public List<UserV2> Users { get; set; } = [];
}

public class DatabaseV3
{
    public int Version { get; set; } = 3;
    public List<UserV3> Users { get; set; } = [];
}

#endregion

#region Migration Infrastructure

public interface IMigration
{
    int FromVersion { get; }
    int ToVersion { get; }

    string Migrate(string json);
}

public class MigrationV1ToV2 : IMigration
{
    public int FromVersion => 1;
    public int ToVersion => 2;

    public string Migrate(string json)
    {
        var oldData =
            JsonSerializer.Deserialize<DatabaseV1>(
                json, jsonOptions)
            ?? throw new InvalidDataException("Invalid V1 data.");

        var newData = new DatabaseV2
        {
            Users = oldData.Users.Select(user => new UserV2
            {
                Id = user.Id,
                Name = user.Name,
                Email = "",
                Age = 0,
                IsActive = true
            }).ToList()
        };

        return JsonSerializer.Serialize(newData, jsonOptions);
    }
}

public class MigrationV2ToV3 : IMigration
{
    public int FromVersion => 2;
    public int ToVersion => 3;

    public string Migrate(string json)
    {
        var oldData =
            JsonSerializer.Deserialize<DatabaseV2>(
                json, jsonOptions)
            ?? throw new InvalidDataException("Invalid V2 data.");

        var newData = new DatabaseV3
        {
            Users = oldData.Users.Select(user => new UserV3
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Age = user.Age,
                IsActive = user.IsActive,
                CreatedAt = DateTime.UtcNow
            }).ToList()
        };

        return JsonSerializer.Serialize(newData, jsonOptions);
    }
}

#endregion

#region Migration Manager

public class MigrationManager
{
    private readonly List<IMigration> _migrations;
    private readonly JsonSerializerOptions _options;
    private readonly int _currentVersion;

    public MigrationManager(
        IEnumerable<IMigration> migrations,
        JsonSerializerOptions options,
        int currentVersion)
    {
        _migrations = migrations.ToList();
        _options = options;
        _currentVersion = currentVersion;
    }

    public string Upgrade(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty(
                "version", out var versionElement))
        {
            throw new InvalidDataException(
                "Version property not found.");
        }

        int version = versionElement.GetInt32();

        if (version > _currentVersion)
        {
            throw new NotSupportedException(
                $"Data version {version} is newer than " +
                $"application version {_currentVersion}.");
        }

        while (version < _currentVersion)
        {
            var migration = _migrations.SingleOrDefault(
                x => x.FromVersion == version);

            if (migration == null)
            {
                throw new InvalidOperationException(
                    $"Migration from version {version} not found.");
            }

            json = migration.Migrate(json);
            version = migration.ToVersion;

            Console.WriteLine(
                $"Migration completed: V{migration.FromVersion} -> V{migration.ToVersion}");
        }

        return json;
    }
}

#endregion

#region Storage

public class UserStorage
{
    private readonly string _filePath;
    private readonly string _tempPath;
    private readonly JsonSerializerOptions _options;

    public UserStorage(
        string filePath,
        string tempPath,
        JsonSerializerOptions options)
    {
        _filePath = filePath;
        _tempPath = tempPath;
        _options = options;
    }

    public async Task<string> ReadAsync()
    {
        return await File.ReadAllTextAsync(_filePath);
    }

    public async Task SaveAtomicallyAsync(string json)
    {
        try
        {
            await using (var stream = new FileStream(
                _tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    JsonDocument.Parse(json).RootElement,
                    _options);

                await stream.FlushAsync();
                stream.Flush(flushToDisk: true);
            }

            // Both files are in the same directory.
            // File.Replace is used when the destination exists.
            if (File.Exists(_filePath))
            {
                File.Replace(_tempPath, _filePath, null);
            }
            else
            {
                File.Move(_tempPath, _filePath);
            }
        }
        finally
        {
            if (File.Exists(_tempPath))
                File.Delete(_tempPath);
        }
    }
}

#endregion

#region Application

var migrations = new List<IMigration>
{
    new MigrationV1ToV2(),
    new MigrationV2ToV3()
};

var storage = new UserStorage(
    DataFile, TempFile, jsonOptions);

var migrationManager = new MigrationManager(
    migrations, jsonOptions, CurrentVersion);

try
{
    // Create sample V1 database on first execution.
    if (!File.Exists(DataFile))
    {
        var oldDatabase = new DatabaseV1
        {
            Users =
            [
                new UserV1
                {
                    Id = 1,
                    Name = "Ali"
                },
                new UserV1
                {
                    Id = 2,
                    Name = "Sara"
                }
            ]
        };

        var oldJson = JsonSerializer.Serialize(
            oldDatabase, jsonOptions);

        await File.WriteAllTextAsync(DataFile, oldJson);
    }

    Console.WriteLine("Checking database version...");

    var json = await storage.ReadAsync();

    // Upgrade data before loading the current model.
    var upgradedJson = migrationManager.Upgrade(json);

    if (upgradedJson != json)
    {
        await storage.SaveAtomicallyAsync(upgradedJson);
        Console.WriteLine("Database upgraded successfully.");
    }
    else
    {
        Console.WriteLine("Database is already up to date.");
    }

    var database = JsonSerializer.Deserialize<DatabaseV3>(
        upgradedJson, jsonOptions)
        ?? throw new InvalidDataException("Invalid database.");

    Console.WriteLine("\nUsers:");

    foreach (var user in database.Users)
    {
        Console.WriteLine(
            $"Id: {user.Id} | Name: {user.Name} | " +
            $"Email: {user.Email} | Active: {user.IsActive} | " +
            $"CreatedAt: {user.CreatedAt:O}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Application Error: {ex.Message}");
}

#endregion
