using System.Collections.ObjectModel;
using System.Windows.Input;
using HQStudio.Services.Site;

namespace HQStudio.ViewModels
{
    public enum UninstallPhase
    {
        Confirm,
        Running,
        Done
    }

    /// <summary>Строка списка этапов в окне удаления.</summary>
    public sealed class UninstallStageItem : BaseViewModel
    {
        private UninstallStageState _state = UninstallStageState.Pending;
        private string _note = "";

        public UninstallStageItem(UninstallStage stage, string title)
        {
            Stage = stage;
            Title = title;
        }

        public UninstallStage Stage { get; }
        public string Title { get; }

        public UninstallStageState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                    OnPropertyChanged(nameof(StateText));
            }
        }

        public string Note
        {
            get => _note;
            set
            {
                if (SetProperty(ref _note, value))
                    OnPropertyChanged(nameof(HasNote));
            }
        }

        public bool HasNote => _note.Length > 0;

        public string StateText => _state switch
        {
            UninstallStageState.Running => "Выполняется",
            UninstallStageState.Done => "Готово",
            UninstallStageState.Skipped => "Пропущено",
            UninstallStageState.Failed => "Ошибка",
            _ => "Ожидает"
        };
    }

    public sealed class UninstallViewModel : BaseViewModel
    {
        private readonly UninstallService _service;
        private readonly string _appDir;
        private readonly SynchronizationContext? _context = SynchronizationContext.Current;
        private readonly object _applyLock = new();

        private UninstallPhase _phase = UninstallPhase.Confirm;
        private bool _deleteData;
        private double _progress;
        private string _headline = "";
        private string _summary = "";
        private bool _hasProblems;

        public UninstallViewModel(UninstallService service, string appDir)
        {
            _service = service;
            _appDir = appDir;

            foreach (var (stage, title) in UninstallService.StageTitles)
                Stages.Add(new UninstallStageItem(stage, title));

            StartCommand = new SiteAsyncCommand(RunAsync, () => Phase == UninstallPhase.Confirm);
            CancelCommand = new RelayCommand(_ => Close(), _ => Phase != UninstallPhase.Running);
        }

        public ObservableCollection<UninstallStageItem> Stages { get; } = new();

        public UninstallPhase Phase
        {
            get => _phase;
            private set
            {
                if (SetProperty(ref _phase, value))
                {
                    OnPropertyChanged(nameof(IsConfirm));
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(IsDone));
                    OnPropertyChanged(nameof(ShowStages));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsConfirm => _phase == UninstallPhase.Confirm;
        public bool IsRunning => _phase == UninstallPhase.Running;
        public bool IsDone => _phase == UninstallPhase.Done;
        public bool ShowStages => _phase != UninstallPhase.Confirm;

        public bool DeleteData
        {
            get => _deleteData;
            set
            {
                if (SetProperty(ref _deleteData, value))
                    OnPropertyChanged(nameof(ShowDataWarning));
            }
        }

        public bool ShowDataWarning => _deleteData;

        public double Progress
        {
            get => _progress;
            private set => SetProperty(ref _progress, value);
        }

        public string Headline
        {
            get => _headline;
            private set => SetProperty(ref _headline, value);
        }

        public string Summary
        {
            get => _summary;
            private set => SetProperty(ref _summary, value);
        }

        public bool HasProblems
        {
            get => _hasProblems;
            private set => SetProperty(ref _hasProblems, value);
        }

        public ICommand StartCommand { get; }
        public ICommand CancelCommand { get; }

        public async Task RunAsync()
        {
            if (Phase != UninstallPhase.Confirm)
                return;

            Phase = UninstallPhase.Running;
            Headline = "Удаляю HQ Studio";
            Progress = 0;
            var options = new UninstallOptions(DeleteData, _appDir);

            UninstallResult result;
            try
            {
                result = await Task.Run(() => _service.RunAsync(options, update => Post(() => Apply(update))));
            }
            catch (Exception ex)
            {
                HasProblems = true;
                Headline = "Не удалось завершить удаление";
                Summary = ex.Message;
                Phase = UninstallPhase.Done;
                return;
            }

            // Итоговые состояния берём из результата: поздние сообщения о ходе не должны их перебить.
            foreach (var final in result.Stages)
                Apply(final);
            Progress = 100;
            HasProblems = result.HasFailures || result.HasWarnings;
            Headline = result.HasFailures
                ? "Удаление завершено с ошибками"
                : result.HasWarnings ? "Готово, но есть замечания" : "Готово";
            Summary = BuildSummary(result);
            Phase = UninstallPhase.Done;
        }

        private string BuildSummary(UninstallResult result)
        {
            var siteRemoved = result.Stages.Any(s => s.Stage == UninstallStage.StopSite && s.State == UninstallStageState.Done);
            var parts = new List<string>
            {
                !DeleteData
                    ? "Программа удалена. Данные сайта (клиенты, заказы) остались на этом компьютере."
                    : siteRemoved ? "Программа и данные сайта удалены." : "Программа удалена."
            };
            foreach (var stage in result.Stages.Where(s => s.State is UninstallStageState.Failed or UninstallStageState.Skipped && s.Note.Length > 0))
                parts.Add(stage.Note);
            parts.Add("Окно можно закрыть: оставшиеся файлы программы исчезнут сами через несколько секунд.");
            return string.Join(Environment.NewLine, parts);
        }

        private void Apply(UninstallStageUpdate update)
        {
            lock (_applyLock)
            {
                var item = Stages.First(s => s.Stage == update.Stage);

                // Состояние этапа только растёт: опоздавшее сообщение «Выполняется» не отменит «Готово».
                if (Rank(update.State) < Rank(item.State))
                    return;

                item.State = update.State;
                item.Note = update.Note;

                var finished = Stages.Count(s => Rank(s.State) == 2);
                var running = Stages.Any(s => s.State == UninstallStageState.Running) ? 0.5 : 0;
                Progress = (finished + running) / Stages.Count * 100;
            }
        }

        private static int Rank(UninstallStageState state) => state switch
        {
            UninstallStageState.Pending => 0,
            UninstallStageState.Running => 1,
            _ => 2
        };

        /// <summary>Окно слушает это событие и закрывается.</summary>
        public event EventHandler? CloseRequested;

        public void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

        private void Post(Action action)
        {
            if (_context != null)
                _context.Post(_ => action(), null);
            else
                action();
        }
    }
}
