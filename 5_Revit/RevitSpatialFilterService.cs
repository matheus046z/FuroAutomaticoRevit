using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace FuroAutomaticoRevit.Revit
{
    public class RevitSpatialFilterService
    {
        private readonly Document _hostDoc;

        public RevitSpatialFilterService(Document hostDoc)
        {
            _hostDoc = hostDoc;
        }

        public IList<Element> GetElementsInView(
            RevitLinkInstance link,
            View3D view,
            IEnumerable<BuiltInCategory> categories)
        {
            Document linkedDoc = link.GetLinkDocument();
            if (linkedDoc == null) return new List<Element>();

            // Get transform from link to host
            Transform transform = link.GetTotalTransform();

            // Get project base point offset
            XYZ basePointOffset = GetProjectBasePointOffset(linkedDoc);

            // Create filter for the view crop box
            var filter = CreateViewFilter(view, transform, basePointOffset);

            if (filter == null) return new List<Element>();

            // Create category filter
            var categoryFilter = new ElementMulticategoryFilter(categories.ToList());

            // Combine filters
            var andFilter = new LogicalAndFilter(filter, categoryFilter);

            return new FilteredElementCollector(linkedDoc)
                .WherePasses(andFilter)
                .WhereElementIsNotElementType()
                .ToList();
        }



        private ElementFilter CreateViewFilter(
            View3D view,
            Transform linkTransform,
            XYZ basePointOffset)
        {
            // Get view crop box
            BoundingBoxXYZ cropBox = view.CropBox;
            if (cropBox == null) return null;

            // Apply base point offset to crop box
            XYZ minPoint = cropBox.Min - basePointOffset;
            XYZ maxPoint = cropBox.Max - basePointOffset;

            // Create outline in host coordinates
            Outline hostOutline = new Outline(minPoint, maxPoint);

            // Transform to link coordinates
            Transform inverseTransform = linkTransform.Inverse;
            Outline linkOutline = TransformOutline(hostOutline, inverseTransform);

            // Create spatial filter
            return new BoundingBoxIntersectsFilter(linkOutline);
        }




        //private ElementFilter CreateViewFilter(
        //    Document linkedDoc,
        //    View3D view,
        //    Transform linkTransform,
        //    XYZ basePointOffset)
        //{
        //    // Get view crop box
        //    BoundingBoxXYZ cropBox = view.CropBox;
        //    if (cropBox == null) return null;

        //    // Apply base point offset to crop box
        //    XYZ minPoint = cropBox.Min + basePointOffset;
        //    XYZ maxPoint = cropBox.Max + basePointOffset;

        //    // Create outline in host coordinates
        //    Outline hostOutline = new Outline(minPoint, maxPoint);

        //    // Transform to link coordinates
        //    Transform inverseTransform = linkTransform.Inverse;
        //    Outline linkOutline = TransformOutline(hostOutline, inverseTransform);

        //    // Create spatial filter
        //    return new BoundingBoxIntersectsFilter(linkOutline);
        //}

        private Outline TransformOutline(Outline outline, Transform transform)
        {
            // Transform all 8 corners of the outline
            var corners = new List<XYZ>
            {
                transform.OfPoint(outline.MinimumPoint),
                transform.OfPoint(new XYZ(outline.MinimumPoint.X, outline.MinimumPoint.Y, outline.MaximumPoint.Z)),
                transform.OfPoint(new XYZ(outline.MinimumPoint.X, outline.MaximumPoint.Y, outline.MinimumPoint.Z)),
                transform.OfPoint(new XYZ(outline.MinimumPoint.X, outline.MaximumPoint.Y, outline.MaximumPoint.Z)),
                transform.OfPoint(new XYZ(outline.MaximumPoint.X, outline.MinimumPoint.Y, outline.MinimumPoint.Z)),
                transform.OfPoint(new XYZ(outline.MaximumPoint.X, outline.MinimumPoint.Y, outline.MaximumPoint.Z)),
                transform.OfPoint(new XYZ(outline.MaximumPoint.X, outline.MaximumPoint.Y, outline.MinimumPoint.Z)),
                transform.OfPoint(outline.MaximumPoint)
            };

            // Create new outline that contains all transformed points
            double minX = corners.Min(p => p.X);
            double minY = corners.Min(p => p.Y);
            double minZ = corners.Min(p => p.Z);
            double maxX = corners.Max(p => p.X);
            double maxY = corners.Max(p => p.Y);
            double maxZ = corners.Max(p => p.Z);

            return new Outline(new XYZ(minX, minY, minZ), new XYZ(maxX, maxY, maxZ));
        }

        private XYZ GetProjectBasePointOffset(Document doc)
        {
            // Get project base point
            var basePoint = new FilteredElementCollector(doc)
                .OfClass(typeof(BasePoint))
                .Cast<BasePoint>()
                .FirstOrDefault(bp => bp.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM) != null);

            if (basePoint == null) return XYZ.Zero;

            double eastWest = basePoint.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsDouble();
            double northSouth = basePoint.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsDouble();
            double elevation = basePoint.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsDouble();

            return new XYZ(eastWest, northSouth, elevation);
        }
    }
}