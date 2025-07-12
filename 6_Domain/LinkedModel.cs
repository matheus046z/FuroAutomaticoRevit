using Autodesk.Revit.DB;
using FuroAutomaticoRevit.UI.ViewModels;

namespace FuroAutomaticoRevit.Domain
{
    public class LinkedModel
    {
        public RvtFile File { get; set; }
        public RevitLinkInstance Instance { get; set; }
        public RevitLinkType Type { get; set; }
        public string ModelType { get; set; }
    }
}