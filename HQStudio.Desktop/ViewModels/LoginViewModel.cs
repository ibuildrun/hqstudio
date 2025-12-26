using HQStudio.Services;
using HQStudio.Views.Dialogs;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;
        private readonly SessionService _sessionService = SessionService.Instance;
        
        // Защита от перебора
        private int _failedAttempts;
        private DateTime _lockoutUntil = DateTime.MinValue;
        private const int MaxAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(1);
        
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoading;
        private bool _isApiConnected;
        private bool _isCheckingConnection = true;

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

        public bool IsCheckingConnection
        {
            get => _isCheckingConnection;
            set => SetProperty(ref _isCheckingConnection, value);
        }

        public bool ShowServerError => !IsApiConnected && !IsCheckingConnection && _settings.UseApi;

        public ICommand LoginCommand { get; }
        public ICommand RetryConnectionCommand { get; }

        public event Action? LoginSuccessful;
        public event Action? ClearPassword;

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(_ => Login(), _ => CanLogin());
            RetryConnectionCommand = new RelayCommand(async _ => await CheckApiConnectionAsync());
            _ = CheckApiConnectionAsync();
        }

        private bool CanLogin()
        {
            if (string.IsNullOrEmpty(Username)) return false;
            if (_settings.UseApi && !IsApiConnected) return false;
            if (IsLockedOut()) return false;
            return true;
        }

        private bool IsLockedOut()
        {
            return DateTime.Now < _lockoutUntil;
        }

        private async Task CheckApiConnectionAsync()
        {
            IsCheckingConnection = true;
            OnPropertyChanged(nameof(ShowServerError));
            
            if (_settings.UseApi)
            {
                IsApiConnected = await _apiService.CheckConnectionAsync();
            }
            else
            {
                IsApiConnected = true;
            }
            
            IsCheckingConnection = false;
            OnPropertyChanged(nameof(ShowServerError));
        }

        private async void Login()
        {
            ErrorMessage = string.Empty;
            
            // Проверка блокировки
            if (IsLockedOut())
            {
                var remaining = (_lockoutUntil - DateTime.Now).Seconds;
                ErrorMessage = $"Слишком много попыток. Подождите {remaining} сек.";
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[Login] Starting login for user: {Username}");
            System.Diagnostics.Debug.WriteLine($"[Login] UseApi: {_settings.UseApi}, IsApiConnected: {IsApiConnected}");
            System.Diagnostics.Debug.WriteLine($"[Login] ApiUrl: {_settings.ApiUrl}");
            
            // Проверяем подключение к серверу
            if (_settings.UseApi && !IsApiConnected)
            {
                ErrorMessage = "Сервер недоступен";
                System.Diagnostics.Debug.WriteLine("[Login] Server not connected");
                return;
            }
            
            IsLoading = true;

            // Try API first if enabled
            if (_settings.UseApi && IsApiConnected)
            {
                System.Diagnostics.Debug.WriteLine("[Login] Attempting API login...");
                var result = await _apiService.LoginAsync(Username, Password);
                System.Diagnostics.Debug.WriteLine($"[Login] API result: {(result != null ? "Success" : "Failed")}");
                
                if (result != null)
                {
                    // Сброс счётчика неудачных попыток
                    _failedAttempts = 0;
                    
                    // Проверяем нужно ли сменить пароль
                    if (result.MustChangePassword)
                    {
                        System.Diagnostics.Debug.WriteLine("[Login] User must change password");
                        IsLoading = false;
                        
                        // Показываем диалог смены пароля
                        // Не устанавливаем Owner, т.к. LoginWindow может закрыться
                        var dialog = new ChangePasswordDialog(isFirstLogin: true, currentPassword: Password);
                        
                        try
                        {
                            if (dialog.ShowDialog() == true)
                            {
                                // Пароль успешно изменён, продолжаем вход
                                CompleteLogin(result);
                            }
                            else
                            {
                                // Пользователь отменил - выходим
                                _apiService.ClearToken();
                                ErrorMessage = "Необходимо сменить пароль для продолжения";
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Login] ChangePasswordDialog error: {ex.Message}");
                            _apiService.ClearToken();
                            ErrorMessage = "Ошибка при смене пароля";
                        }
                        return;
                    }
                    
                    CompleteLogin(result);
                    return;
                }
                else
                {
                    // Увеличиваем счётчик неудачных попыток
                    _failedAttempts++;
                    System.Diagnostics.Debug.WriteLine($"[Login] Failed attempt {_failedAttempts}/{MaxAttempts}");
                    
                    if (_failedAttempts >= MaxAttempts)
                    {
                        _lockoutUntil = DateTime.Now.Add(LockoutDuration);
                        ErrorMessage = $"Слишком много попыток. Подождите {LockoutDuration.TotalSeconds:0} сек.";
                    }
                    else
                    {
                        var remaining = MaxAttempts - _failedAttempts;
                        ErrorMessage = $"Неверный логин или пароль (осталось {remaining} попыток)";
                    }
                    
                    // Очищаем пароль при неверном вводе
                    Password = string.Empty;
                    ClearPassword?.Invoke();
                    
                    IsLoading = false;
                    return;
                }
            }

            // Fallback to local auth (only if API is disabled)
            await Task.Delay(300);
            if (_dataService.Login(Username, Password))
            {
                _failedAttempts = 0;
                // Start session with local user ID
                if (_dataService.CurrentUser != null)
                {
                    await _sessionService.StartSessionAsync(_dataService.CurrentUser.Id);
                }
                LoginSuccessful?.Invoke();
            }
            else
            {
                _failedAttempts++;
                if (_failedAttempts >= MaxAttempts)
                {
                    _lockoutUntil = DateTime.Now.Add(LockoutDuration);
                    ErrorMessage = $"Слишком много попыток. Подождите {LockoutDuration.TotalSeconds:0} сек.";
                }
                else
                {
                    ErrorMessage = "Неверный логин или пароль";
                }
                
                // Очищаем пароль при неверном вводе
                Password = string.Empty;
                ClearPassword?.Invoke();
            }

            IsLoading = false;
        }

        private async void CompleteLogin(LoginResult result)
        {
            try
            {
                // Set local user for compatibility
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login] CompleteLogin error: {ex.Message}");
                IsLoading = false;
                ErrorMessage = "Ошибка при входе в систему";
            }
        }
    }
}
