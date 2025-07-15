using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace FuroAutomaticoRevit.Revit
{
    public class RevitFilterService
    {
        private readonly Document _doc;

        public RevitFilterService(Document doc)
        {
            _doc = doc;
        }

        public IList<Element> GetVisiblePipes(RevitLinkInstance link)
        {
            // Filter by category instead of specific types
            return new FilteredElementCollector(link.GetLinkDocument())
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WhereElementIsNotElementType()
                .ToList();
        }

        public IList<Element> GetVisibleConduits(RevitLinkInstance link)
        {
            return new FilteredElementCollector(link.GetLinkDocument())
                .OfCategory(BuiltInCategory.OST_Conduit)
                .WhereElementIsNotElementType()
                .ToList();
        }

        public IList<Element> GetVisibleSlabs(RevitLinkInstance link)
        {
            return GetVisibleElements(link, new List<BuiltInCategory> {
                BuiltInCategory.OST_Floors
            });
        }

        private IList<Element> GetVisibleElements(
            RevitLinkInstance link,
            List<BuiltInCategory> categories)
        {
            Document linkedDoc = link.GetLinkDocument();
            if (linkedDoc == null) return new List<Element>();

            // Create category filter
            var categoryFilter = new ElementMulticategoryFilter(categories);

            // Collect all elements (remove view filtering)
            return new FilteredElementCollector(linkedDoc)
                .WhereElementIsNotElementType()
                .WherePasses(categoryFilter)
                .ToList();
        }

        public View GetViewByName(string viewName)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .FirstOrDefault(v => v.Name.Equals(viewName));
        }
    }
}