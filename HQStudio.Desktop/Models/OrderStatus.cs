using System.Windows.Media;

namespace HQStudio.Models
{
    /// <summary>
    /// Централизованная система статусов заказов.
    /// Единый источник истины для всех статусов в приложении.
    /// </summary>
    public class OrderStatus
    {
        /// <summary>API код статуса (0-3)</summary>
        public int Code { get; init; }
        
        /// <summary>Отображаемое название на русском</summary>
        public string DisplayName { get; init; } = "";
        
        /// <summary>Английское название для API</summary>
        public string ApiName { get; init; } = "";
        
        /// <summary>Цвет текста статуса (HEX)</summary>
        public string TextColor { get; init; } = "#FFFFFF";
        
        /// <summary>Цвет фона строки (HEX)</summary>
        public string BackgroundColor { get; init; } = "#1E1E1E";
        
        /// <summary>Иконка статуса</summary>
        public string Icon { get; init; } = "";
        
        /// <summary>Можно ли редактировать заказ с этим статусом</summary>
        public bool IsEditable { get; init; } = true;
        
        /// <summary>Учитывается ли в выручке</summary>
        public bool CountsAsRevenue { get; init; }
        
        /// <summary>Активный заказ (не завершён и не отменён)</summary>
        public bool IsActive { get; init; }

        // Предопределённые статусы
        public static readonly OrderStatus New = new()
        {
            Code = 0,
            DisplayName = "Новый",
            ApiName = "New",
            TextColor = "#2196F3",
            BackgroundColor = "#1A2A3A",
            Icon = "🆕",
            IsEditable = true,
            CountsAsRevenue = false,
            IsActive = true
        };

        public static readonly OrderStatus InProgress = new()
        {
            Code = 1,
            DisplayName = "В работе",
            ApiName = "InProgress",
            TextColor = "#FFC107",
            BackgroundColor = "#2A2A1A",
            Icon = "🔧",
            IsEditable = true,
            CountsAsRevenue = false,
            IsActive = true
        };

        public static readonly OrderStatus Completed = new()
        {
            Code = 2,
            DisplayName = "Завершен",
            ApiName = "Completed",
            TextColor = "#4CAF50",
            BackgroundColor = "#1A2A1A",
            Icon = "✅",
            IsEditable = false,
            CountsAsRevenue = true,
            IsActive = false
        };

        public static readonly OrderStatus Cancelled = new()
        {
            Code = 3,
            DisplayName = "Отменен",
            ApiName = "Cancelled",
            TextColor = "#F44336",
            BackgroundColor = "#2A1A1A",
            Icon = "❌",
            IsEditable = false,
            CountsAsRevenue = false,
            IsActive = false
        };

        /// <summary>Все доступные статусы</summary>
        public static readonly OrderStatus[] All = { New, InProgress, Completed, Cancelled };

        /// <summary>Статусы для выбора в UI (все)</summary>
        public static readonly OrderStatus[] SelectableStatuses = { New, InProgress, Completed, Cancelled };

        /// <summary>Активные статусы (для фильтрации)</summary>
        public static readonly OrderStatus[] ActiveStatuses = { New, InProgress };

        /// <summary>Получить статус по API коду</summary>
        public static OrderStatus FromCode(int code) => code switch
        {
            0 => New,
            1 => InProgress,
            2 => Completed,
            3 => Cancelled,
            _ => New
        };

        /// <summary>Получить статус по отображаемому названию</summary>
        public static OrderStatus FromDisplayName(string displayName) => displayName switch
        {
            "Новый" => New,
            "В работе" => InProgress,
            "Завершен" => Completed,
            "Отменен" => Cancelled,
            _ => New
        };

        /// <summary>Получить статус по API названию</summary>
        public static OrderStatus FromApiName(string apiName) => apiName switch
        {
            "New" => New,
            "InProgress" => InProgress,
            "Completed" => Completed,
            "Cancelled" => Cancelled,
            _ => New
        };

        /// <summary>Получить Brush для цвета текста</summary>
        public Brush GetTextBrush() => new SolidColorBrush((Color)ColorConverter.ConvertFromString(TextColor));

        /// <summary>Получить Brush для фона</summary>
        public Brush GetBackgroundBrush() => new SolidColorBrush((Color)ColorConverter.ConvertFromString(BackgroundColor));

        public override string ToString() => DisplayName;
    }
}
