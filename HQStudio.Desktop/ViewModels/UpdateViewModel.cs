using System.Windows.Input;
using HQStudio.Services;

namespace HQStudio.ViewModels
{
    public class UpdateViewModel : BaseViewModel
    {
        private readonly UpdateService _updateService;

        private bool _isChecking;
        private bool _isDownloading;
        private bool _updateAvailable;
        private double _downloadProgress;
        private string _statusMessage = "";
        private string _currentVersion = "";
        private string _newVersion = "";
        private string _releaseNotes = "";
        private string _fileSize = "";
        private bool _isMandatory;

        public bool IsChecking
        {
            get => _isChecking;
            set => SetProperty(ref _isChecking, value);
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set => SetProperty(ref _isDownloading, value);
        }

        public bool UpdateAvailable
        {
            get => _updateAvailable;
            set => SetProperty(ref _updateAvailable, value);
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set => SetProperty(ref _downloadProgress, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string CurrentVersion
        {
            get => _currentVersion;
            set => SetProperty(ref _currentVersion, value);
        }

        public string NewVersion
        {
            get => _newVersion;
            set => SetProperty(ref _newVersion, value);
        }

        public string ReleaseNotes
        {
            get => _releaseNotes;
            set => SetProperty(ref _releaseNotes, value);
        }

        public string FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        public bool IsMandatory
        {
            get => _isMandatory;
            set => SetProperty(ref _isMandatory, value);
        }

        public ICommand CheckUpdatesCommand { get; }
        public ICommand InstallUpdateCommand { get; }

        public UpdateViewModel()
        {
            _updateService = UpdateService.Instance;
            CurrentVersion = _updateService.CurrentVersion;

            _updateService.UpdateStatusChanged += (s, msg) => StatusMessage = msg;
            _updateService.DownloadProgressChanged += (s, progress) => DownloadProgress = progress;
            _updateService.UpdateAvailable += OnUpdateAvailable;

            CheckUpdatesCommand = new RelayCommand(async _ => await CheckForUpdatesAsync(), _ => !IsChecking && !IsDownloading);
            InstallUpdateCommand = new RelayCommand(async _ => await InstallUpdateAsync(), _ => UpdateAvailable && !IsDownloading);
        }

        private void OnUpdateAvailable(object? sender, UpdateInfo update)
        {
            UpdateAvailable = true;
            NewVersion = update.Version;
            ReleaseNotes = update.ReleaseNotes;
            FileSize = update.FileSizeFormatted;
            IsMandatory = update.IsMandatory;
        }

        public async Task CheckForUpdatesAsync()
        {
            IsChecking = true;
            UpdateAvailable = false;

            try
            {
                var update = await _updateService.CheckForUpdatesAsync();
                if (update == null)
                {
                    StatusMessage = "У вас установлена последняя версия";
                }
            }
            finally
            {
                IsChecking = false;
            }
        }

        public async Task InstallUpdateAsync()
        {
            IsDownloading = true;
            DownloadProgress = 0;

            try
            {
                await _updateService.DownloadAndInstallAsync();
            }
            finally
            {
                IsDownloading = false;
            }
        }

        /// <summary>
        /// Автоматическая проверка при старте (вызывается из MainWindow)
        /// </summary>
        public async Task CheckOnStartupAsync()
        {
            await CheckForUpdatesAsync();
        }
    }
}
