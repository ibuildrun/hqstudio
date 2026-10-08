using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using HQStudio.Services.Guide;

namespace HQStudio.Controls
{
    /// <summary>Текст инструкции: участки между <c>**</c> выделяются жирным.</summary>
    public sealed class GuideTextBlock : TextBlock
    {
        public static readonly DependencyProperty MarkupProperty = DependencyProperty.Register(
            nameof(Markup), typeof(string), typeof(GuideTextBlock),
            new PropertyMetadata("", (d, _) => ((GuideTextBlock)d).Rebuild()));

        public GuideTextBlock()
        {
            TextWrapping = TextWrapping.Wrap;
        }

        public string Markup
        {
            get => (string)GetValue(MarkupProperty);
            set => SetValue(MarkupProperty, value);
        }

        private void Rebuild()
        {
            Inlines.Clear();
            foreach (var part in GuideMarkup.Parse(Markup))
            {
                var run = new Run(part.Text);
                if (part.Bold)
                {
                    run.FontWeight = FontWeights.SemiBold;
                    // Ссылка на ресурс, а не кисть: цвет меняется вместе с темой.
                    run.SetResourceReference(TextElement.ForegroundProperty, "TextPrimaryBrush");
                }
                Inlines.Add(run);
            }
        }
    }
}
