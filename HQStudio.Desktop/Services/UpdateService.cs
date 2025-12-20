using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;

namespace HQStudio.Services
{
    public class UpdateService
    {
        private static UpdateService? _instance;
        public static UpdateService Instance => _instance ??= new UpdateService();

        private readonly HttpClient _http;
        private readonly string _baseUrl;

        public string CurrentVersion { get; }
        public UpdateInfo? AvailableUpdate { get; private set; }
        public bool IsUpdateAvailable => AvailableUpdate != null;

        public event EventHandler<UpdateInfo>? UpdateAvailable;
        public event EventHandler<double>? DownloadProgressChanged;
        public event EventHandler<string>? UpdateStatusChanged;

        private UpdateService()
        {
            _baseUrl = SettingsService.Instance.ApiUrl;
            _http = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            
            // Получаем текущую версию из AssemblyInfo
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            CurrentVersion = version != null 
                ? $"{version.Major}.{version.Minor}.{version.Build}" 
                : "1.0.0";
        }

        /// <summary>
        /// Проверить наличие обновлений
        /// </summary>
        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                UpdateStatusChanged?.Invoke(this, "Проверка обновлений...");
                
                var response = await _http.GetFromJsonAsync<UpdateCheckResponse>(
                    $"/api/updates/check?currentVersion={CurrentVersion}");

                if (response != null && response.UpdateAvailable)
                {
                    AvailableUpdate = new UpdateInfo
                    {
                        Version = response.LatestVersion ?? "",
                        ReleaseNotes = response.ReleaseNotes ?? "",
                        DownloadUrl = response.DownloadUrl ?? "",
                        FileSize = response.FileSize ?? 0,
                        Checksum = response.Checksum ?? "",
                        IsMandatory = response.IsMandatory,
                        ReleasedAt = response.ReleasedAt ?? DateTime.Now
                    };

                    UpdateAvailable?.Invoke(this, AvailableUpdate);
                    UpdateStatusChanged?.Invoke(this, $"Доступна версия {AvailableUpdate.Version}");
                    return AvailableUpdate;
                }

                UpdateStatusChanged?.Invoke(this, "У вас последняя версия");
                AvailableUpdate = null;
                return null;
            }
            catch (Exception ex)
            {
                UpdateStatusChanged?.Invoke(this, $"Ошибка проверки: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Скачать и установить обновление
        /// </summary>
        public async Task<bool> DownloadAndInstallAsync()
        {
            if (AvailableUpdate == null)
                return false;

            try
            {
                UpdateStatusChanged?.Invoke(this, "Скачивание обновления...");

                // Путь для временного файла
                var tempPath = Path.Combine(Path.GetTempPath(), "HQStudio_Update");
                Directory.CreateDirectory(tempPath);
                var updateFile = Path.Combine(tempPath, $"HQStudio_{AvailableUpdate.Version}.exe");

                // Скачиваем файл с прогрессом
                using var response = await _http.GetAsync(
                    AvailableUpdate.DownloadUrl, 
                    HttpCompletionOption.ResponseHeadersRead);
                
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? AvailableUpdate.FileSize;
                var downloadedBytes = 0L;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(updateFile, FileMode.Create, FileAccess.Write, FileShare.None);
                
                var buffer = new byte[8192];
                int bytesRead;
                
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;
                    
                    var progress = totalBytes > 0 ? (double)downloadedBytes / totalBytes * 100 : 0;
                    DownloadProgressChanged?.Invoke(this, progress);
                }

                UpdateStatusChanged?.Invoke(this, "Проверка целостности...");

                // Проверяем контрольную сумму
                if (!string.IsNullOrEmpty(AvailableUpdate.Checksum))
                {
                    var fileChecksum = await ComputeChecksumAsync(updateFile);
                    if (!fileChecksum.Equals(AvailableUpdate.Checksum, StringComparison.OrdinalIgnoreCase))
                    {
                        UpdateStatusChanged?.Invoke(this, "Ошибка: файл повреждён");
                        File.Delete(updateFile);
                        return false;
                    }
                }

                UpdateStatusChanged?.Invoke(this, "Установка обновления...");

                // Создаём скрипт для обновления (заменяет exe после закрытия приложения)
                var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
                if (currentExe == null)
                {
                    UpdateStatusChanged?.Invoke(this, "Ошибка: не удалось определить путь приложения");
                    return false;
                }

                var updateScript = Path.Combine(tempPath, "update.bat");
                var scriptContent = $@"
@echo off
echo Ожидание закрытия HQ Studio...
timeout /t 2 /nobreak > nul
:waitloop
tasklist /FI ""IMAGENAME eq HQStudio.exe"" 2>NUL | find /I /N ""HQStudio.exe"">NUL
if ""%ERRORLEVEL%""==""0"" (
    timeout /t 1 /nobreak > nul
    goto waitloop
)
echo Установка обновления...
copy /Y ""{updateFile}"" ""{currentExe}""
echo Запуск обновлённой версии...
start """" ""{currentExe}""
del ""{updateScript}""
";
                await File.WriteAllTextAsync(updateScript, scriptContent);

                // Запускаем скрипт обновления
                Process.Start(new ProcessStartInfo
                {
                    FileName = updateScript,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                UpdateStatusChanged?.Invoke(this, "Перезапуск приложения...");
                
                // Закрываем текущее приложение
                System.Windows.Application.Current.Shutdown();
                
                return true;
            }
            catch (Exception ex)
            {
                UpdateStatusChanged?.Invoke(this, $"Ошибка установки: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Вычисление SHA256 контрольной суммы
        /// </summary>
        private static async Task<string> ComputeChecksumAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    // DTOs для обновлений
    public class UpdateCheckResponse
    {
        public bool UpdateAvailable { get; set; }
        public string? LatestVersion { get; set; }
        public string? CurrentVersion { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? DownloadUrl { get; set; }
        public long? FileSize { get; set; }
        public string? Checksum { get; set; }
        public bool IsMandatory { get; set; }
        public DateTime? ReleasedAt { get; set; }
    }

    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public long FileSize { get; set; }
        public string Checksum { get; set; } = "";
        public bool IsMandatory { get; set; }
        public DateTime ReleasedAt { get; set; }

        public string FileSizeFormatted => FileSize switch
        {
            < 1024 => $"{FileSize} B",
            < 1024 * 1024 => $"{FileSize / 1024.0:F1} KB",
            _ => $"{FileSize / 1024.0 / 1024.0:F1} MB"
        };
    }
}
