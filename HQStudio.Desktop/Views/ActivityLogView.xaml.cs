using System.Windows.Controls;
using HQStudio.ViewModels;

namespace HQStudio.Views
{
    public partial class ActivityLogView : UserControl
    {
        public ActivityLogView()
        {
            InitializeComponent();
            DataContext = new ActivityLogViewModel();
        }
    }
}
