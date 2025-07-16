using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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
            TaskDialog.Show("Debug", $"Link transform: Origin={transform.Origin}, " +
                $"BasisX={transform.BasisX}, BasisY={transform.BasisY}, BasisZ={transform.BasisZ}");

            // Get project base point offset
            XYZ basePointOffset = GetProjectBasePointOffset(linkedDoc);
            TaskDialog.Show("Debug", $"Base point offset: {basePointOffset}");


            // Create filter for the view section box
            var filter = CreateSectionBoxFilter(view, transform, basePointOffset);
            if (filter == null)
            {
                TaskDialog.Show("Debug", "Section box filter is null - using all elements");
                return new FilteredElementCollector(linkedDoc)
                    .OfCategory(categories.First())
                    .WhereElementIsNotElementType()
                    .ToList();
            }

            // Create category filter
            var categoryFilter = new ElementMulticategoryFilter(categories.ToList());

            // Combine filters
            var andFilter = new LogicalAndFilter(filter, categoryFilter);

            var elements = new FilteredElementCollector(linkedDoc)
                .WherePasses(andFilter)
                .WhereElementIsNotElementType()
                .ToList();
            
            TaskDialog.Show("Debug", $"Found {elements.Count} elements in view after filtering");

            return elements;

        }


        private ElementFilter CreateSectionBoxFilter(
            View3D view,
            Transform linkTransform,
            XYZ basePointOffset)
        {
            if (!view.IsSectionBoxActive) return null;

            BoundingBoxXYZ sectionBox = view.GetSectionBox();
            if (sectionBox == null) return null;

            // Get transform from host to link
            Transform inverseTransform = linkTransform.Inverse;

            // Transform all corners of the section box
            XYZ[] corners = new[]
            {
                sectionBox.Min,
                new XYZ(sectionBox.Min.X, sectionBox.Min.Y, sectionBox.Max.Z),
                new XYZ(sectionBox.Min.X, sectionBox.Max.Y, sectionBox.Min.Z),
                new XYZ(sectionBox.Min.X, sectionBox.Max.Y, sectionBox.Max.Z),
                new XYZ(sectionBox.Max.X, sectionBox.Min.Y, sectionBox.Min.Z),
                new XYZ(sectionBox.Max.X, sectionBox.Min.Y, sectionBox.Max.Z),
                new XYZ(sectionBox.Max.X, sectionBox.Max.Y, sectionBox.Min.Z),
                sectionBox.Max
            };

            // Transform corners to link's coordinate system
            XYZ[] transformedCorners = corners
                .Select(corner => inverseTransform.OfPoint(corner - basePointOffset))
                .ToArray();

            // Create bounding box in link's coordinates
            double minX = transformedCorners.Min(p => p.X);
            double minY = transformedCorners.Min(p => p.Y);
            double minZ = transformedCorners.Min(p => p.Z);
            double maxX = transformedCorners.Max(p => p.X);
            double maxY = transformedCorners.Max(p => p.Y);
            double maxZ = transformedCorners.Max(p => p.Z);

            Outline linkOutline = new Outline(
                new XYZ(minX, minY, minZ),
                new XYZ(maxX, maxY, maxZ)
            );

            TaskDialog.Show("Debug", $"Transformed section box: Min={linkOutline.MinimumPoint}, Max={linkOutline.MaximumPoint}");

            return new BoundingBoxIntersectsFilter(linkOutline);
        }


        //    private ElementFilter CreateSectionBoxFilter(
        //View3D view,
        //Transform linkTransform,
        //XYZ basePointOffset)
        //    {
        //        // Verify section box is active
        //        if (!view.IsSectionBoxActive)
        //        {
        //            TaskDialog.Show("Debug", "Section box is not active");
        //            return null;
        //        }

        //        // Get section box
        //        BoundingBoxXYZ sectionBox = view.GetSectionBox();
        //        if (sectionBox == null)
        //        {
        //            TaskDialog.Show("Debug", "Section box is null");
        //            return null;
        //        }

        //        TaskDialog.Show("Debug", $"Original section box: Min={sectionBox.Min}, Max={sectionBox.Max}");

        //        // Apply base point offset to section box
        //        XYZ minPoint = sectionBox.Min - basePointOffset;
        //        XYZ maxPoint = sectionBox.Max - basePointOffset;

        //        TaskDialog.Show("Debug", $"Section box after base point offset: Min={minPoint}, Max={maxPoint}");

        //        // Create outline in host coordinates
        //        Outline hostOutline = new Outline(minPoint, maxPoint);

        //        // Transform to link coordinates
        //        Transform inverseTransform = linkTransform.Inverse;
        //        Outline linkOutline = TransformOutline(hostOutline, inverseTransform);

        //        TaskDialog.Show("Debug", $"Link outline: Min={linkOutline.MinimumPoint}, Max={linkOutline.MaximumPoint}");

        //        // Create spatial filter
        //        return new BoundingBoxIntersectsFilter(linkOutline);
        //    }

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

            if (basePoint == null)
            {
                TaskDialog.Show("Debug", "Project base point not found");
                return XYZ.Zero;
            }

            double eastWest = basePoint.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsDouble();
            double northSouth = basePoint.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsDouble();
            double elevation = basePoint.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsDouble();

            var offset = new XYZ(eastWest, northSouth, elevation);
            TaskDialog.Show("Debug", $"Project base point: {offset}");
            return offset;
        }
    }
}