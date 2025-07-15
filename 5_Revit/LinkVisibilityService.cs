using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;

namespace FuroAutomaticoRevit.Revit
{
    public class LinkVisibilityService
    {
        private readonly Document _doc;
        private readonly UIDocument _uiDoc;

        public LinkVisibilityService(Document doc, UIDocument uiDoc)
        {
            _doc = doc;
            _uiDoc = uiDoc;
        }

        public void EnsureLinkVisibility(RevitLinkInstance linkInstance)
        {
            View activeView = _uiDoc.ActiveView;
            if (activeView == null) return;

            Category linkCategory = Category.GetCategory(_doc, BuiltInCategory.OST_RvtLinks);
            if (activeView.GetCategoryHidden(linkCategory.Id))
            {
                //activeView.SetCategoryHidden(linkCategory.Id, false);
                using (Transaction t = new Transaction(_doc, "Show Links Category"))
                {
                    t.Start();
                    activeView.SetCategoryHidden(linkCategory.Id, false);
                    t.Commit();
                }
            }
        }
    }
}