using System.IO;
using System.IO.Compression;

namespace HQStudio.Services.Updates
{
    public static class ArchiveExtractor
    {
        /// <summary>
        /// Extracts a zip into <paramref name="destination"/> (created if missing). Entries escaping the
        /// destination are rejected; a single wrapper folder is stripped when the archive has no root-level
        /// compose file. Returns the relative paths written. Entries rejected by <paramref name="skip"/> are not written.
        /// </summary>
        public static IReadOnlyList<string> ExtractToDirectory(string zipPath, string destination,
            Func<string, bool>? skip = null)
        {
            try
            {
                using var zip = ZipFile.OpenRead(zipPath);
                var files = zip.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToList();
                var strip = DetectWrapperFolder(files);

                var root = Path.GetFullPath(destination);
                Directory.CreateDirectory(root);
                var rootWithSep = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;

                var written = new List<string>();
                foreach (var entry in files)
                {
                    var relative = entry.FullName.Replace('\\', '/');
                    if (strip.Length > 0)
                        relative = relative[strip.Length..];
                    relative = relative.TrimStart('/');

                    if (relative.Length == 0 || (skip?.Invoke(relative) ?? false))
                        continue;

                    var target = Path.GetFullPath(Path.Combine(root, relative));
                    if (!target.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
                        throw new UpdateException("Архив обновления содержит недопустимые пути и отклонён.");

                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    entry.ExtractToFile(target, overwrite: true);
                    written.Add(relative);
                }
                return written;
            }
            catch (InvalidDataException ex)
            {
                throw new UpdateException("Скачанный архив повреждён. Повторите попытку.", ex);
            }
        }

        /// <summary>Reads one file (by name, at any depth) out of a zip into <paramref name="destinationFile"/>.</summary>
        public static void ExtractSingleFile(string zipPath, string fileName, string destinationFile)
        {
            try
            {
                using var zip = ZipFile.OpenRead(zipPath);
                var entry = zip.Entries
                    .Where(e => string.Equals(e.Name, fileName, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(e => e.FullName.Count(c => c is '/' or '\\'))
                    .FirstOrDefault();
                if (entry == null)
                    throw new UpdateException($"В скачанном архиве нет файла {fileName}. Обновление отменено.");

                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                entry.ExtractToFile(destinationFile, overwrite: true);
            }
            catch (InvalidDataException ex)
            {
                throw new UpdateException("Скачанный архив повреждён. Повторите попытку.", ex);
            }
        }

        private static string DetectWrapperFolder(List<ZipArchiveEntry> files)
        {
            if (files.Count == 0)
                return "";

            var hasRootCompose = files.Any(e => string.Equals(e.FullName.Replace('\\', '/'),
                "docker-compose.yml", StringComparison.OrdinalIgnoreCase));
            if (hasRootCompose)
                return "";

            var first = files[0].FullName.Replace('\\', '/');
            var slash = first.IndexOf('/');
            if (slash <= 0)
                return "";

            var prefix = first[..(slash + 1)];
            return files.All(e => e.FullName.Replace('\\', '/').StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                ? prefix
                : "";
        }
    }
}
