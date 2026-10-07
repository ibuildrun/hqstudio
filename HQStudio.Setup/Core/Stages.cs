namespace HQStudio.Setup.Core;

public enum StageId
{
    Prepare,
    Configure,
    DockerReady,
    Pull,
    Start,
    Health,
    PublicUrl,
    Shortcuts
}

public enum StageState
{
    Pending,
    Running,
    Done,
    Warning,
    Failed,
    Skipped
}

public static class StageNames
{
    public static string Title(StageId id) => id switch
    {
        StageId.Prepare => "Подготовка и копирование программы",
        StageId.Configure => "Настройка сайта",
        StageId.DockerReady => "Запуск Docker",
        StageId.Pull => "Скачивание сайта",
        StageId.Start => "Запуск сайта",
        StageId.Health => "Проверка работы",
        StageId.PublicUrl => "Публичный адрес",
        StageId.Shortcuts => "Ярлыки",
        _ => id.ToString()
    };

    /// <summary>Accepts the enum name or a short alias used by the --simulate-fail flag.</summary>
    public static bool TryParse(string? text, out StageId id)
    {
        id = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var key = text.Trim().ToLowerInvariant();
        switch (key)
        {
            case "docker": id = StageId.DockerReady; return true;
            case "tunnel": case "tuna": case "public": id = StageId.PublicUrl; return true;
            case "copy": id = StageId.Prepare; return true;
            case "shortcut": case "links": id = StageId.Shortcuts; return true;
        }
        return Enum.TryParse(text.Trim(), ignoreCase: true, out id) && Enum.IsDefined(id);
    }
}
