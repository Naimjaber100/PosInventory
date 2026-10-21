using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;

namespace Inventory
{
    // "Backup" in appsettings.json:
    //   "Backup": { "Folder": "", "PgDumpPath": "", "KeepLast": 30 }
    // Folder empty = Documents\Inventory backups. PgDumpPath empty = the newest pg_dump.exe of
    // C:\Program Files\PostgreSQL\<version>\bin. KeepLast: older backups of this database are deleted (0 = keep all).
    public sealed class BackupSettings
    {
        public const string Section = "Backup";

        public string Folder { get; set; } = string.Empty;
        public string PgDumpPath { get; set; } = string.Empty;
        public int KeepLast { get; set; } = 30;

        public string EffectiveFolder => string.IsNullOrWhiteSpace(Folder)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Inventory backups")
            : Environment.ExpandEnvironmentVariables(Folder.Trim());

        public static BackupSettings Read(JsonElement root)
        {
            if (!root.TryGetProperty(Section, out var section) || section.ValueKind != JsonValueKind.Object) return new BackupSettings();
            try { return section.Deserialize<BackupSettings>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new BackupSettings(); }
            catch (JsonException) { return new BackupSettings(); }
        }

        /// <summary>Writes the "Backup" section of appsettings.json next to the program (the other sections are kept).</summary>
        public void Save()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            JsonObject root;
            try { root = File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject() : new JsonObject(); }
            catch (JsonException) { root = new JsonObject(); }
            root[Section] = new JsonObject { ["Folder"] = Folder.Trim(), ["PgDumpPath"] = PgDumpPath.Trim(), ["KeepLast"] = KeepLast };
            try
            {
                File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                throw new InvalidOperationException($"Could not write {path}:\n{ex.Message}");
            }
        }
    }

    // Settings > Database backup: the whole database (tables, data, functions) with PostgreSQL's pg_dump,
    // in its own format (.backup), which pg_restore reads back:
    //   pg_restore -h localhost -U postgres -d <new empty database> "Pos_2026-10-20_1530.backup"
    public static class DatabaseBackup
    {
        public const string Extension = ".backup";

        /// <summary>pg_dump.exe: the one in appsettings.json, else the newest PostgreSQL install on this computer; null when none.</summary>
        public static string? FindPgDump()
        {
            var configured = AppConfig.Backup.PgDumpPath.Trim();
            if (configured.Length > 0)
            {
                var file = Directory.Exists(configured) ? Path.Combine(configured, "pg_dump.exe") : configured;
                return File.Exists(file) ? file : null;
            }

            var found = new List<(int Version, string Path)>();
            foreach (var root in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 })
            {
                var folder = Path.Combine(Environment.GetFolderPath(root), "PostgreSQL");
                if (!Directory.Exists(folder)) continue;
                foreach (var dir in Directory.GetDirectories(folder))
                {
                    var exe = Path.Combine(dir, "bin", "pg_dump.exe");
                    if (File.Exists(exe)) found.Add((int.TryParse(Path.GetFileName(dir).Split('.')[0], out var v) ? v : 0, exe));
                }
            }
            return found.OrderByDescending(f => f.Version).Select(f => f.Path).FirstOrDefault();
        }

        /// <summary>The database name of the connection string ("Pos").</summary>
        public static string DatabaseName => new NpgsqlConnectionStringBuilder(AppConfig.ConnectionString).Database ?? "database";

        /// <summary>The backups of this database in the folder, newest first.</summary>
        public static List<FileInfo> List(string folder) =>
            Directory.Exists(folder)
                ? new DirectoryInfo(folder).GetFiles($"{DatabaseName}_*{Extension}").OrderByDescending(f => f.LastWriteTime).ToList()
                : new List<FileInfo>();

        /// <summary>Backs up the database into the folder; returns the new file. Then keeps only the last KeepLast backups.</summary>
        public static async Task<FileInfo> RunAsync(string folder, CancellationToken cancel = default)
        {
            var pgDump = FindPgDump() ?? throw new InvalidOperationException(
                "pg_dump.exe was not found on this computer.\n\nInstall the PostgreSQL command line tools, or put its path in appsettings.json:\n" +
                "\"Backup\": { \"PgDumpPath\": \"C:\\\\Program Files\\\\PostgreSQL\\\\15\\\\bin\\\\pg_dump.exe\" }");
            var cs = new NpgsqlConnectionStringBuilder(AppConfig.ConnectionString);

            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, $"{DatabaseName}_{DateTime.Now:yyyy-MM-dd_HHmmss}{Extension}");
            var start = new ProcessStartInfo(pgDump)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = Encoding.UTF8,
            };
            // Custom format (compressed, restored with pg_restore); --no-password: never wait for a password prompt
            foreach (var arg in new[] { "--format=custom", "--no-password", "--file", file,
                                        "--host", cs.Host ?? "localhost", "--port", cs.Port.ToString(), "--username", cs.Username ?? "postgres", cs.Database ?? "Pos" })
                start.ArgumentList.Add(arg);
            if (!string.IsNullOrEmpty(cs.Password)) start.Environment["PGPASSWORD"] = cs.Password;   // else pgpass.conf / trust, as the program itself

            using var process = Process.Start(start) ?? throw new InvalidOperationException("pg_dump could not be started.");
            var errors = process.StandardError.ReadToEndAsync(cancel);
            try
            {
                await process.WaitForExitAsync(cancel);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(); } catch { /* already ended */ }
                TryDelete(file);
                throw;
            }
            if (process.ExitCode != 0)
            {
                TryDelete(file);
                var message = (await errors).Trim();
                if (message.Contains("server version", StringComparison.OrdinalIgnoreCase))
                    message += "\n\nThis pg_dump is older than the database server: use the pg_dump of the same (or a newer) PostgreSQL version (Backup.PgDumpPath in appsettings.json).";
                else if (message.Contains("password", StringComparison.OrdinalIgnoreCase))
                    message += "\n\nThe database asks for a password: add Password=... to the connection string in appsettings.json.";
                throw new InvalidOperationException("The backup failed:\n" + message);
            }

            var keep = AppConfig.Backup.KeepLast;
            if (keep > 0) foreach (var old in List(folder).Skip(keep)) TryDelete(old.FullName);
            return new FileInfo(file);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* left for the user */ }
        }
    }
}
