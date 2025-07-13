using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FuroAutomaticoRevit.UI.Utils;
using FuroAutomaticoRevit.UI.Views;
using System;
using System.Windows;
using System.Windows.Interop;

namespace FuroAutomaticoRevit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class CreateHolesCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                // Referencias de aplicação e documento
                UIApplication uiApp = commandData.Application;
                var mainView = new MainView(uiApp);

                var revitWindow = new WindowInteropHelper(mainView);
                revitWindow.Owner = uiApp.MainWindowHandle;

                mainView.Topmost = true;
                Win32Helper.MakeTopMost(mainView);
                
                
                mainView.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}