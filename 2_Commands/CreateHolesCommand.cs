using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FuroAutomaticoRevit.UI.Views;
using System;
using System.Windows;

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