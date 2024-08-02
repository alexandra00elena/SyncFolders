using System;
using System.IO;
using System.Threading;
using System.Timers;

class Program
{
    static string? sourcePath;
    static string? replicaPath;
    static int syncInterval;
    static string? logFilePath;
    static System.Timers.Timer? syncTimer;

    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: SyncFolders <sourcePath> <replicaPath> <syncInterval> <logFilePath>");
            return;
        }

        sourcePath = args[0];
        replicaPath = args[1];
        syncInterval = int.Parse(args[2]);
        logFilePath = args[3];

        // Initialize and start the timer
        syncTimer = new System.Timers.Timer(syncInterval * 1000);
        syncTimer.Elapsed += OnTimedEvent;
        syncTimer.AutoReset = true;
        syncTimer.Enabled = true;

        // Perform initial synchronization
        SynchronizeFolders();

        // Keep the application running
        Console.WriteLine("Press [Enter] to exit the program.");
        Console.ReadLine();
    }

    private static void OnTimedEvent(Object? source, ElapsedEventArgs e)
    {
        SynchronizeFolders();
    }

    private static void SynchronizeFolders()
    {
        try
        {
            // Ensure replica folder exists
            Directory.CreateDirectory(replicaPath);

            // Get list of file paths in source and replica
            var sourceFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
            var replicaFiles = Directory.GetFiles(replicaPath, "*", SearchOption.AllDirectories);

            // Copy and update files from source to replica
            foreach (var sourceFile in sourceFiles)
            {
                var relativePath = Path.GetRelativePath(sourcePath, sourceFile);
                var replicaFile = Path.Combine(replicaPath, relativePath);

                if (!File.Exists(replicaFile) || File.GetLastWriteTimeUtc(sourceFile) > File.GetLastWriteTimeUtc(replicaFile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(replicaFile));
                    File.Copy(sourceFile, replicaFile, true);
                    Log($"Copied/Updated: {sourceFile} to {replicaFile}");
                }
            }

            // Delete files from replica that are not in source
            foreach (var replicaFile in replicaFiles)
            {
                var relativePath = Path.GetRelativePath(replicaPath, replicaFile);
                var sourceFile = Path.Combine(sourcePath, relativePath);

                if (!File.Exists(sourceFile))
                {
                    File.Delete(replicaFile);
                    Log($"Deleted: {replicaFile}");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private static void Log(string message)
    {
        var logMessage = $"{DateTime.Now}: {message}";
        Console.WriteLine(logMessage);

        try
        {
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write to log file: {ex.Message}");
        }
    }
}
