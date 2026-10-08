using System.Collections.ObjectModel;
using System.Windows.Input;
using HQStudio.Services.Guide;
using HQStudio.Services.Site;

namespace HQStudio.ViewModels
{
    /// <summary>Глава инструкции в списке выбора.</summary>
    public sealed class GuideChoice : BaseViewModel
    {
        private bool _isSelected;

        public GuideChoice(string id, string title)
        {
            Id = id;
            Title = title;
        }

        public string Id { get; }
        public string Title { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    /// <summary>
    /// Окно «Инструкция». Само окно открывает только администратор (проверка в <see cref="SiteViewModel"/>
    /// и при показе окна); здесь только листание глав и ссылки.
    /// </summary>
    public sealed class SiteGuideViewModel : BaseViewModel
    {
        private readonly IReadOnlyList<GuideSection> _sections;
        private readonly ISiteShell _shell;
        private readonly ISiteNotifier _notifier;
        private int _index;

        public SiteGuideViewModel(IReadOnlyList<GuideSection> sections, ISiteShell shell, ISiteNotifier notifier,
            string? startSectionId = null)
        {
            if (sections.Count == 0)
                throw new ArgumentException("В инструкции нет глав.", nameof(sections));

            _sections = sections;
            _shell = shell;
            _notifier = notifier;

            foreach (var section in sections)
                Choices.Add(new GuideChoice(section.Id, section.Title));

            var start = startSectionId == null ? -1 : IndexOf(startSectionId);
            _index = start < 0 ? 0 : start;
            UpdateSelection();

            SelectCommand = new RelayCommand(p =>
            {
                if (p is string id)
                    Select(id);
            });
            NextCommand = new RelayCommand(_ => Go(_index + 1), _ => CanGoNext);
            PreviousCommand = new RelayCommand(_ => Go(_index - 1), _ => CanGoPrevious);
            OpenLinkCommand = new RelayCommand(p =>
            {
                if (p is string url)
                    OpenLink(url);
            });
            ReportBugCommand = new RelayCommand(_ => ReportBugRequested?.Invoke());
        }

        /// <summary>Окно слушает событие и показывает форму «Сообщить об ошибке».</summary>
        public event Action? ReportBugRequested;

        public ObservableCollection<GuideChoice> Choices { get; } = new();

        public GuideSection SelectedSection => _sections[_index];
        public IReadOnlyList<GuideStep> Steps => SelectedSection.Steps;
        public string Title => SelectedSection.Title;
        public string Summary => SelectedSection.Summary;
        public string Intro => SelectedSection.Intro;
        public bool HasIntro => SelectedSection.HasIntro;
        public bool ShowReportBug => SelectedSection.Id == SiteGuideContent.BugReportId;
        public string ProgressText => $"Глава {_index + 1} из {_sections.Count}";

        public bool CanGoNext => _index < _sections.Count - 1;
        public bool CanGoPrevious => _index > 0;

        public ICommand SelectCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand OpenLinkCommand { get; }
        public ICommand ReportBugCommand { get; }

        public bool Select(string id)
        {
            var index = IndexOf(id);
            if (index < 0)
                return false;
            Go(index);
            return true;
        }

        private int IndexOf(string id)
        {
            for (var i = 0; i < _sections.Count; i++)
            {
                if (string.Equals(_sections[i].Id, id, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        private void Go(int index)
        {
            if (index < 0 || index >= _sections.Count || index == _index)
                return;

            _index = index;
            UpdateSelection();
            foreach (var name in new[]
                     {
                         nameof(SelectedSection), nameof(Steps), nameof(Title), nameof(Summary), nameof(Intro),
                         nameof(HasIntro), nameof(ShowReportBug), nameof(ProgressText), nameof(CanGoNext), nameof(CanGoPrevious)
                     })
                OnPropertyChanged(name);
            CommandManager.InvalidateRequerySuggested();
        }

        private void UpdateSelection()
        {
            for (var i = 0; i < Choices.Count; i++)
                Choices[i].IsSelected = i == _index;
        }

        private void OpenLink(string url)
        {
            if (!_shell.OpenUrl(url))
                _notifier.Warning("Не получилось открыть браузер. Скопируйте адрес и вставьте его в браузер вручную.");
        }
    }
}
