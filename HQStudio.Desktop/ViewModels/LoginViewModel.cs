using HQStudio.Services;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;
        private readonly SessionService _sessionService = SessionService.Instance;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoading;
        private bool _isApiConnected;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsApiConnected
        {
            get => _isApiConnected;
            set => SetProperty(ref _isApiConnected, value);
        }

        public ICommand LoginCommand { get; }

        public event Action? LoginSuccessful;

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(_ => Login(), _ => !string.IsNullOrEmpty(Username));
            CheckApiConnection();
        }

        private async void CheckApiConnection()
        {
            if (_settings.UseApi)
            {
                IsApiConnected = await _apiService.CheckConnectionAsync();
            }
        }

        private async void Login()
        {
            ErrorMessage = string.Empty;
            IsLoading = true;

            // Try API first if enabled
            if (_settings.UseApi && IsApiConnected)
            {
                var result = await _apiService.LoginAsync(Username, Password);
                if (result != null)
                {
                    // Also set local user for compatibility
                    _dataService.CurrentUser = new Models.User
                    {
                        Id = result.User.Id,
                        Username = result.User.Login,
                        DisplayName = result.User.Name,
                        Role = result.User.Role
                    };
                    
                    // Start session for online status tracking
                    await _sessionService.StartSessionAsync(result.User.Id);
                    
                    IsLoading = false;
                    LoginSuccessful?.Invoke();
                    return;
                }
            }

            // Fallback to local auth
            await Task.Delay(300);
            if (_dataService.Login(Username, Password))
            {
                // Start session with local user ID
                if (_dataService.CurrentUser != null)
                {
                    await _sessionService.StartSessionAsync(_dataService.CurrentUser.Id);
                }
                LoginSuccessful?.Invoke();
            }
            else
            {
                ErrorMessage = "Неверный логин или пароль";
            }

            IsLoading = false;
        }
    }
}
