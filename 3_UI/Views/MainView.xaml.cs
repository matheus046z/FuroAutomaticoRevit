using Autodesk.Revit.UI;
using FuroAutomaticoRevit.UI.ViewModels;
using System.Windows;

namespace FuroAutomaticoRevit.UI.Views
{
    public partial class MainView : Window
    {
        public MainView(UIApplication uiApp)
        {
            InitializeComponent();
            DataContext = new MainViewModel(uiApp);
        }
    }
}