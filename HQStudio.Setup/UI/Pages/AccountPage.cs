using HQStudio.Setup.Core;

namespace HQStudio.Setup.UI.Pages;

public sealed class AccountPageViewModel : PageViewModel
{
    private string _firstName = "";
    private string _lastName = "";
    private string _password = "";
    private string _passwordRepeat = "";
    private bool _showPassword;
    private bool _attempted;
    private readonly HashSet<string> _touched = new();

    public AccountPageViewModel(IWizardHost host) : base(host) { }

    public override WizardStep Step => WizardStep.Account;
    public override string Title => "Главный аккаунт";
    public override string Subtitle => "С этого аккаунта вы будете входить в программу и управлять сайтом.";

    /// <summary>Raised with the field name when Next is pressed and that field is the first invalid one.</summary>
    public event Action<string>? FocusRequested;

    public string FirstName
    {
        get => _firstName;
        set { if (Set(ref _firstName, value)) { RaiseErrors(); } }
    }

    public string LastName
    {
        get => _lastName;
        set { if (Set(ref _lastName, value)) { RaiseErrors(); } }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (!Set(ref _password, value))
                return;
            Raise(nameof(Strength));
            Raise(nameof(StrengthText));
            RaiseErrors();
        }
    }

    public string PasswordRepeat
    {
        get => _passwordRepeat;
        set { if (Set(ref _passwordRepeat, value)) { RaiseErrors(); } }
    }

    public bool ShowPassword
    {
        get => _showPassword;
        set => Set(ref _showPassword, value);
    }

    /// <summary>0 = empty, 1 = weak, 2 = medium, 3 = strong; drives the three-segment bar.</summary>
    public int Strength => (int)Validators.Strength(Password);

    public string StrengthText => Validators.StrengthText(Validators.Strength(Password));

    // Errors appear after the field was left once, or after Next was pressed.
    public string? FirstNameError => Visible(nameof(FirstName), Validators.FirstName(FirstName));
    public string? LastNameError => Visible(nameof(LastName), Validators.LastName(LastName));
    public string? PasswordError => Visible(nameof(Password), Validators.Password(Password, FirstName, LastName));
    public string? PasswordRepeatError =>
        Visible(nameof(PasswordRepeat), PasswordRepeat.Length == 0 && !_attempted ? null : Validators.PasswordRepeat(Password, PasswordRepeat));

    public bool HasFirstNameError => FirstNameError != null;
    public bool HasLastNameError => LastNameError != null;
    public bool HasPasswordError => PasswordError != null;
    public bool HasPasswordRepeatError => PasswordRepeatError != null;

    public bool IsValid =>
        Validators.FirstName(FirstName) == null &&
        Validators.LastName(LastName) == null &&
        Validators.Password(Password, FirstName, LastName) == null &&
        Validators.PasswordRepeat(Password, PasswordRepeat) == null;

    public void Touch(string field)
    {
        if (_touched.Add(field))
            RaiseErrors();
    }

    public override void OnEntered()
    {
        Primary.Show("Далее", TryAdvance);
        Secondary.Show("Назад", Host.Back);
        Tertiary.Hide();
    }

    public void TryAdvance()
    {
        _attempted = true;
        RaiseErrors();

        if (!IsValid)
        {
            var first = Validators.FirstName(FirstName) != null ? nameof(FirstName)
                : Validators.LastName(LastName) != null ? nameof(LastName)
                : Validators.Password(Password, FirstName, LastName) != null ? nameof(Password)
                : nameof(PasswordRepeat);
            FocusRequested?.Invoke(first);
            return;
        }

        var answers = Host.Answers;
        answers.FirstName = FirstName.Trim();
        answers.LastName = LastName.Trim();
        answers.Password = Password;
        Host.Next();
    }

    /// <summary>For the page preview: shows every validation message at once.</summary>
    internal void ShowAllErrors()
    {
        _attempted = true;
        RaiseErrors();
    }

    private string? Visible(string field, string? error) => error != null && (_attempted || _touched.Contains(field)) ? error : null;

    private void RaiseErrors()
    {
        Raise(nameof(FirstNameError));
        Raise(nameof(LastNameError));
        Raise(nameof(PasswordError));
        Raise(nameof(PasswordRepeatError));
        Raise(nameof(HasFirstNameError));
        Raise(nameof(HasLastNameError));
        Raise(nameof(HasPasswordError));
        Raise(nameof(HasPasswordRepeatError));
    }
}
