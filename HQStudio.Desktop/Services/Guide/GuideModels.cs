namespace HQStudio.Services.Guide
{
    public sealed record GuideLink(string Text, string Url);

    /// <summary>Строка таблицы внутри шага: что заполнить и что писать.</summary>
    public sealed record GuideRow(string Label, string Value);

    /// <summary>
    /// Один пронумерованный шаг. В текстах <c>**так**</c> выделяется жирным (названия кнопок и полей).
    /// </summary>
    public sealed record GuideStep(
        string Title,
        string Text = "",
        IReadOnlyList<string>? Bullets = null,
        IReadOnlyList<GuideRow>? Rows = null,
        IReadOnlyList<GuideLink>? Links = null,
        string Note = "")
    {
        public int Number { get; init; }

        public IReadOnlyList<string> BulletList => Bullets ?? Array.Empty<string>();
        public IReadOnlyList<GuideRow> RowList => Rows ?? Array.Empty<GuideRow>();
        public IReadOnlyList<GuideLink> LinkList => Links ?? Array.Empty<GuideLink>();

        public bool HasText => Text.Length > 0;
        public bool HasBullets => BulletList.Count > 0;
        public bool HasRows => RowList.Count > 0;
        public bool HasLinks => LinkList.Count > 0;
        public bool HasNote => Note.Length > 0;

        /// <summary>Весь видимый текст шага: для проверок содержимого.</summary>
        public IEnumerable<string> AllText()
        {
            yield return Title;
            yield return Text;
            foreach (var bullet in BulletList)
                yield return bullet;
            foreach (var row in RowList)
            {
                yield return row.Label;
                yield return row.Value;
            }
            foreach (var link in LinkList)
                yield return link.Text;
            yield return Note;
        }
    }

    public sealed record GuideSection(
        string Id,
        string Title,
        string Summary,
        IReadOnlyList<GuideStep> Steps,
        string Intro = "")
    {
        public bool HasIntro => Intro.Length > 0;

        public IEnumerable<string> AllText()
        {
            yield return Title;
            yield return Summary;
            yield return Intro;
            foreach (var step in Steps)
            {
                foreach (var text in step.AllText())
                    yield return text;
            }
        }
    }

    public readonly record struct GuideRun(string Text, bool Bold);

    /// <summary>Разбор простой разметки: участки между парами <c>**</c> выделяются жирным.</summary>
    public static class GuideMarkup
    {
        public static IReadOnlyList<GuideRun> Parse(string? text)
        {
            var runs = new List<GuideRun>();
            if (string.IsNullOrEmpty(text))
                return runs;

            var parts = text.Split("**");
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    runs.Add(new GuideRun(parts[i], i % 2 == 1));
            }
            return runs;
        }

        /// <summary>Текст без разметки.</summary>
        public static string Strip(string? text) => string.Concat(Parse(text).Select(r => r.Text));

        /// <summary>Каждая открывающая пара <c>**</c> должна закрываться, иначе остаток строки окажется жирным.</summary>
        public static bool IsBalanced(string? text) =>
            string.IsNullOrEmpty(text) || (text.Split("**").Length - 1) % 2 == 0;
    }
}
