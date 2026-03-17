using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace TopSpeed.Updater
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                var options = ParseArgs(args);
                WaitForProcessExit(options.ProcessId);
                InstallZip(options);
                StartGame(options);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static UpdaterOptions ParseArgs(string[] args)
        {
            var options = new UpdaterOptions();
            for (var i = 0; i < args.Length; i++)
            {
                var key = args[i] ?? string.Empty;
                var value = i + 1 < args.Length ? (args[i + 1] ?? string.Empty) : string.Empty;
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                switch (key)
                {
                    case "--pid":
                        if (int.TryParse(value, out var pid))
                        {
                            options.ProcessId = pid;
                            i++;
                        }
                        break;
                    case "--zip":
                        options.ZipPath = value;
                        i++;
                        break;
                    case "--dir":
                        options.TargetDir = value;
                        i++;
                        break;
                    case "--game":
                        options.GameExeName = value;
                        i++;
                        break;
                    case "--skip":
                        options.SkipFileName = value;
                        i++;
                        break;
                }
            }

            if (options.ProcessId <= 0)
                throw new InvalidOperationException("Missing or invalid --pid argument.");
            if (string.IsNullOrWhiteSpace(options.ZipPath))
                throw new InvalidOperationException("Missing --zip argument.");
            if (string.IsNullOrWhiteSpace(options.TargetDir))
                throw new InvalidOperationException("Missing --dir argument.");
            if (string.IsNullOrWhiteSpace(options.GameExeName))
                throw new InvalidOperationException("Missing --game argument.");

            return options;
        }

        private static void WaitForProcessExit(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                process.WaitForExit();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        private static void InstallZip(UpdaterOptions options)
        {
            var zipPath = Path.GetFullPath(options.ZipPath);
            var targetDir = Path.GetFullPath(options.TargetDir);
            if (!File.Exists(zipPath))
                throw new FileNotFoundException("Update zip was not found.", zipPath);
            if (!Directory.Exists(targetDir))
                throw new DirectoryNotFoundException($"Target directory was not found: {targetDir}");

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                for (var i = 0; i < archive.Entries.Count; i++)
                {
                    var entry = archive.Entries[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.FullName))
                        continue;
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    if (!string.IsNullOrWhiteSpace(options.SkipFileName) &&
                        string.Equals(entry.Name, options.SkipFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var destination = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));
                    if (!destination.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Unsafe entry path: {entry.FullName}");

                    var parent = Path.GetDirectoryName(destination);
                    if (!string.IsNullOrWhiteSpace(parent))
                        Directory.CreateDirectory(parent);

                    entry.ExtractToFile(destination, overwrite: true);
                }
            }

            File.Delete(zipPath);
        }

        private static void StartGame(UpdaterOptions options)
        {
            var gamePath = ResolveGamePath(options.TargetDir, options.GameExeName);
            if (string.IsNullOrWhiteSpace(gamePath) || !File.Exists(gamePath))
                throw new FileNotFoundException("Updated game executable was not found.", Path.Combine(options.TargetDir, options.GameExeName));

            var workingDirectory = Path.GetDirectoryName(gamePath);
            if (string.IsNullOrWhiteSpace(workingDirectory))
                workingDirectory = options.TargetDir;

            Process.Start(new ProcessStartInfo
            {
                FileName = gamePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true
            });
        }

        private static string ResolveGamePath(string targetDir, string gameExeName)
        {
            var directPath = Path.Combine(targetDir, gameExeName);
            if (File.Exists(directPath))
                return directPath;

            var matches = Directory.GetFiles(targetDir, gameExeName, SearchOption.AllDirectories);
            if (matches.Length == 0)
                return directPath;
            if (matches.Length == 1)
                return matches[0];

            var bestMatch = matches[0];
            var bestDepth = GetPathDepth(bestMatch);
            for (var i = 1; i < matches.Length; i++)
            {
                var candidate = matches[i];
                var candidateDepth = GetPathDepth(candidate);
                if (candidateDepth < bestDepth)
                {
                    bestMatch = candidate;
                    bestDepth = candidateDepth;
                }
            }

            return bestMatch;
        }

        private static int GetPathDepth(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return int.MaxValue;

            var fullPath = Path.GetFullPath(path);
            var root = Path.GetPathRoot(fullPath) ?? string.Empty;
            var relative = fullPath.Substring(root.Length);
            if (relative.Length == 0)
                return 0;

            var segments = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            return segments.Length;
        }


        private sealed class UpdaterOptions
        {
            public int ProcessId { get; set; }
            public string ZipPath { get; set; } = string.Empty;
            public string TargetDir { get; set; } = string.Empty;
            public string GameExeName { get; set; } = string.Empty;
            public string SkipFileName { get; set; } = string.Empty;
        }
    }
}
