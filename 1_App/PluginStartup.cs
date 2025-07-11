
using Autodesk.Revit.UI;
using FuroAutomaticoRevit.Commands;

namespace FuroAutomaticoRevit.App
{
    public class PluginStartup : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {

            try
            {
                application.CreateRibbonTab("Furos Automaticos");
            }
            catch
            {

            }


            // Criar Painel
            RibbonPanel panel = application.CreateRibbonPanel(
                "Furos Automaticos",
                "Ferramenta de aberturas em laje");

            // Criar Botão
            var button = new PushButtonData(
                "CreateHolesCommand",
                "Executar Plugin",
                typeof(CreateHolesCommand).Assembly.Location,
                typeof(CreateHolesCommand).FullName);

            panel.AddItem(button);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            
            return Result.Succeeded;
        }
    }
}