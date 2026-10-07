using System.IO.Compression;

namespace HQStudio.Setup.Core;

public sealed record PayloadResult(IReadOnlyList<string> AppFiles, IReadOnlyList<string> ServerFiles);

/// <summary>
/// Unpacks payload.zip: "app/" goes to the program folder, "server/" to the server folder. Every entry path is
/// checked before anything is written, so a hostile archive cannot escape the target folders.
/// </summary>
public static class PayloadExtractor
{
    public const string AppPrefix = "app/";
    public const string ServerPrefix = "server/";

    private sealed record PlannedFile(ZipArchiveEntry Entry, string Target, string Display, bool IsApp);

    public static PayloadResult Extract(Stream zipStream, string appDir, string serverDir, Action<double, string>? progress = null)
    {
        ZipArchive zip;
        try
        {
            zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException ex)
        {
            throw new InstallException(FailureKind.PayloadCorrupt, "Архив с файлами программы не читается.", inner: ex);
        }

        using (zip)
        {
            var plan = Plan(zip, Path.GetFullPath(appDir), Path.GetFullPath(serverDir));

            if (!plan.Any(p => p.IsApp && p.Display.Equals(InstallPaths.AppExeName, StringComparison.OrdinalIgnoreCase)))
                throw new InstallException(FailureKind.PayloadCorrupt, $"В архиве нет файла {InstallPaths.AppExeName}.");

            var app = new List<string>();
            var server = new List<string>();
            for (var i = 0; i < plan.Count; i++)
            {
                var file = plan[i];
                progress?.Invoke((double)i / plan.Count, file.Display);
                WriteFile(file);
                (file.IsApp ? app : server).Add(file.Display);
            }
            progress?.Invoke(1.0, "");
            return new PayloadResult(app, server);
        }
    }

    private static List<PlannedFile> Plan(ZipArchive zip, string appRoot, string serverRoot)
    {
        var result = new List<PlannedFile>();
        foreach (var entry in zip.Entries)
        {
            var name = entry.FullName.Replace('\\', '/');
            if (name.EndsWith('/') || string.IsNullOrEmpty(entry.Name))
                continue;

            if (IsUnsafe(name))
                throw new InstallException(FailureKind.PayloadCorrupt, "Архив содержит недопустимые пути и отклонён.", details: entry.FullName);

            bool isApp;
            string relative;
            string root;
            if (name.StartsWith(AppPrefix, StringComparison.OrdinalIgnoreCase))
            {
                isApp = true;
                relative = name[AppPrefix.Length..];
                root = appRoot;
            }
            else if (name.StartsWith(ServerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                isApp = false;
                relative = name[ServerPrefix.Length..];
                root = serverRoot;
            }
            else
            {
                continue;
            }

            // The user's .env holds database secrets and must never be replaced by the archive.
            if (!isApp && relative.Equals(".env", StringComparison.OrdinalIgnoreCase))
                continue;

            var target = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            var rootWithSep = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
            if (!target.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
                throw new InstallException(FailureKind.PayloadCorrupt, "Архив содержит недопустимые пути и отклонён.", details: entry.FullName);

            result.Add(new PlannedFile(entry, target, relative, isApp));
        }
        return result;
    }

    private static bool IsUnsafe(string name)
    {
        if (name.StartsWith('/') || name.Contains(':'))
            return true;
        return name.Split('/').Any(segment => segment == "..");
    }

    private static void WriteFile(PlannedFile file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file.Target)!);
        var temp = file.Target + ".part";
        try
        {
            using (var input = file.Entry.Open())
            using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                input.CopyTo(output);
            File.Move(temp, file.Target, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch (IOException) { }
        }
    }
}
