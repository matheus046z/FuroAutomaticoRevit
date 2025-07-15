using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FuroAutomaticoRevit.Domain;
using FuroAutomaticoRevit.Revit;
using FuroAutomaticoRevit.UI.Views;
using System.Collections.Generic;
using System.Linq;

namespace FuroAutomaticoRevit.Core
{
    public class IntersectionService
    {
        private readonly Document _doc;
        private readonly RevitFilterService _filterService;
        private const double TOLERANCE = 0.001;
        private const string SANITARY_PIPE_TYPE = "Tubo - Esgoto - Série Normal";
        private const string ELECTRICAL_CONDUIT_TYPE = "Eletroduto em ferro galvanizado";
        private const string STRUCTURAL_SLAB_TYPE = "Generic Floor - 400mm";
        // checar nome dos tipos. Bug esta impedindo a aplicação dos filtros de tipo


        public IntersectionService(Document doc)
        {
            _doc = doc;
            _filterService = new RevitFilterService(doc);
        }

        public IList<IntersectionData> FindIntersections(
            RevitLinkInstance mepLink,
            RevitLinkInstance structuralLink,
            Outline hostViewOutline)
        {
            const string TARGET_VIEW = "Vista teste";
            var results = new List<IntersectionData>();

            // Achar vista teste
            View targetView = _filterService.GetViewByName(TARGET_VIEW);
            if (targetView == null)
            {
                TaskDialog.Show("Erro", $"A vista '{TARGET_VIEW}' não foi encontrada!");
                return results;
            }

            // Get transforms for coordinate conversion
            Transform mepTransform = mepLink.GetTotalTransform();
            Transform structuralTransform = structuralLink.GetTotalTransform();

            TaskDialog.Show("Debug - Transform Check",
            $"MEP Link Origin: {mepTransform.Origin}\n" +
            $"Structural Link Origin: {structuralTransform.Origin}\n" +
            $"View Min: {hostViewOutline.MinimumPoint}\n" +
            $"View Max: {hostViewOutline.MaximumPoint}");

            // Transform view outline to each link's coordinate system
            Transform mepInverse = mepTransform.Inverse;
            Outline mepViewOutline = new Outline(
                mepInverse.OfPoint(hostViewOutline.MinimumPoint),
                mepInverse.OfPoint(hostViewOutline.MaximumPoint)
            );

            Transform structuralInverse = structuralTransform.Inverse;
            Outline structuralViewOutline = new Outline(
                structuralInverse.OfPoint(hostViewOutline.MinimumPoint),
                structuralInverse.OfPoint(hostViewOutline.MaximumPoint)
            );

            // Achar cropbox da vista
            BoundingBoxXYZ viewCropBox = targetView.CropBox;
            if (viewCropBox == null)
            {
                TaskDialog.Show("Erro", "A vista nao tem crop box!");
                return results;
            }

            // Criar filtro
            //Outline viewOutline = new Outline(viewCropBox.Min, viewCropBox.Max);
            BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(hostViewOutline);


            //DEBUG

            TaskDialog.Show("Debug - Type Filters",
            $"Sanitary Pipe Type: {SANITARY_PIPE_TYPE}\n" +
            $"Electrical Conduit Type: {ELECTRICAL_CONDUIT_TYPE}\n" +
            $"Structural Slab Type: {STRUCTURAL_SLAB_TYPE}");


            TaskDialog.Show("Debug - Transformed Outlines",
            $"MEP View Outline Min: {mepViewOutline.MinimumPoint}\n" +
            $"MEP View Outline Max: {mepViewOutline.MaximumPoint}\n" +
            $"Structural View Outline Min: {structuralViewOutline.MinimumPoint}\n" +
            $"Structural View Outline Max: {structuralViewOutline.MaximumPoint}");


            // After getting transforms
            TaskDialog.Show("Debug - Transform Check",
            $"MEP Link Origin: {mepTransform.Origin}\n" +
            $"Structural Link Origin: {structuralTransform.Origin}\n" +
            $"Host View Min: {hostViewOutline.MinimumPoint}\n" +
            $"Host View Max: {hostViewOutline.MaximumPoint}");

            // After creating outlines
            TaskDialog.Show("Debug - Transformed Outlines",
            $"MEP View Min: {mepViewOutline.MinimumPoint}\n" +
            $"MEP View Max: {mepViewOutline.MaximumPoint}\n" +
            $"Structural View Min: {structuralViewOutline.MinimumPoint}\n" +
            $"Structural View Max: {structuralViewOutline.MaximumPoint}");

            //bool bypassSpatialFilter = false; // DESATIVA a filtragem espacial para debug
            //bool bypassTypeFilter = true; // DESATIVA a filtragem de tipo para debug

            //DEBUG




            //// SEM FILTROS

            //// Get ALL pipes, conduits and slabs without any filtering
            //var pipes = _filterService.GetVisiblePipes(mepLink);
            //var conduits = _filterService.GetVisibleConduits(mepLink);
            //var slabs = _filterService.GetVisibleSlabs(structuralLink);

            //// SEM FILTROS





            // Get filtered elements with spatial filtering
            var pipes = FilterSpecificTypes(
                _filterService.GetVisiblePipes(mepLink)
                    .Where(e => IsElementInView(e, mepTransform, hostViewOutline)),
                SANITARY_PIPE_TYPE
            );

            var conduits = FilterSpecificTypes(
                _filterService.GetVisibleConduits(mepLink)
                    .Where(e => IsElementInView(e, mepTransform, hostViewOutline)),
                ELECTRICAL_CONDUIT_TYPE
            );

            var slabs = FilterSpecificTypes(
                _filterService.GetVisibleSlabs(structuralLink)
                    .Where(e => IsElementInView(e, structuralTransform, hostViewOutline)),
                STRUCTURAL_SLAB_TYPE
            );

            //DEBUG
            TaskDialog.Show("Debug - Element Counts",
            $"Total Pipes: {_filterService.GetVisiblePipes(mepLink).Count}\n" +
            $"Pipes after filters: {pipes.Count}\n" +
            $"Total Conduits: {_filterService.GetVisibleConduits(mepLink).Count}\n" +
            $"Conduits after filters: {conduits.Count}\n" +
            $"Total Slabs: {_filterService.GetVisibleSlabs(structuralLink).Count}\n" +
            $"Slabs after filters: {slabs.Count}");


            TaskDialog.Show("Slab Debug",
            $"Slabs found: {slabs.Count}\n" +
            string.Join("\n", slabs.Select(s =>
                $"ID: {s.Id.IntegerValue}, " +
                $"Type: {s.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM)?.AsValueString()}, " +
                $"Name: {s.Name}")));

            //DEBUG

            // Combine pipes and conduits
            var allPipes = new List<Element>();
            allPipes.AddRange(pipes);
            allPipes.AddRange(conduits);

            int intersectionCount = 0; //DEBUG

            // Process intersections
            foreach (var pipe in allPipes)
            {
                Solid pipeSolid = GeometryUtils.GetElementSolid(pipe, mepTransform);
                if (pipeSolid == null || pipeSolid.Volume < TOLERANCE) continue;

                foreach (var slab in slabs)
                {
                    Solid slabSolid = GeometryUtils.GetElementSolid(slab, structuralTransform);
                    if (slabSolid == null || slabSolid.Volume < TOLERANCE) continue;

                    // Get intersection solid
                    Solid intersectionSolid = GeometryUtils.GetIntersectionSolid(pipeSolid, slabSolid);

                    if (intersectionSolid != null && intersectionSolid.Volume > TOLERANCE)
                    {
                        XYZ centroid = GeometryUtils.GetCentroid(intersectionSolid);

                        intersectionCount++; //DEBUG

                        results.Add(new IntersectionData
                        {
                            Pipe = pipe,
                            Slab = slab,
                            Location = centroid,
                            PipeDiameter = GetPipeDiameter(pipe),
                            SlabThickness = GetSlabThickness(slab),
                            IntersectionSolid = intersectionSolid
                        });
                    }
                }
            }

            //DEBUG
            TaskDialog.Show("Debug - Intersections",
            $"Potential intersections found: {intersectionCount}");
            //DEBUG

            return results;
        }

        private bool IsElementInView(Element element, Transform transform, Outline viewOutline)
        {
            BoundingBoxXYZ bbox = GeometryUtils.GetElementBoundingBox(element);
            if (bbox == null)
            {
                TaskDialog.Show("Debug - Missing BBox", $"Element {element.Id} has no bounding box");
                return false;
            }

            // Create a more accurate bounding box using all corners
            XYZ min = bbox.Min;
            XYZ max = bbox.Max;

            XYZ[] corners = new[]
            {
            new XYZ(min.X, min.Y, min.Z),
            new XYZ(min.X, min.Y, max.Z),
            new XYZ(min.X, max.Y, min.Z),
            new XYZ(min.X, max.Y, max.Z),
            new XYZ(max.X, min.Y, min.Z),
            new XYZ(max.X, min.Y, max.Z),
            new XYZ(max.X, max.Y, min.Z),
            new XYZ(max.X, max.Y, max.Z)
    };

            // Transform all corners to host coordinates
            XYZ[] transformedCorners = corners
                .Select(corner => transform.OfPoint(corner))
                .ToArray();

            // Calculate new bounding box from transformed points
            double newMinX = transformedCorners.Min(p => p.X);
            double newMinY = transformedCorners.Min(p => p.Y);
            double newMinZ = transformedCorners.Min(p => p.Z);
            double newMaxX = transformedCorners.Max(p => p.X);
            double newMaxY = transformedCorners.Max(p => p.Y);
            double newMaxZ = transformedCorners.Max(p => p.Z);

            Outline elementOutline = new Outline(
                new XYZ(newMinX, newMinY, newMinZ),
                new XYZ(newMaxX, newMaxY, newMaxZ)
            );



            const double TOLERANCE = 0.01;

            bool inView = viewOutline.Intersects(elementOutline, TOLERANCE);

            TaskDialog.Show($"Debug - Element {element.Id}",
                $"Element Min: {min}\n" +
                $"Element Max: {max}\n" +
                $"View Min: {viewOutline.MinimumPoint}\n" +
                $"View Max: {viewOutline.MaximumPoint}\n" +
                $"In View: {inView}");

            return viewOutline.Intersects(elementOutline, TOLERANCE);
        }

        private List<Element> FilterSpecificTypes(IEnumerable<Element> elements, string typeName)
        {
            return elements
                .Where(e =>
                    e.Name?.Contains(typeName) == true ||
                    e.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM)?.AsValueString()?.Contains(typeName) == true ||
                    e.Category?.Name?.Contains(typeName) == true
                )
                .ToList();
        }

        private double GetPipeDiameter(Element pipe)
        {
            Parameter param = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM) ??
                              pipe.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM);

            return param?.AsDouble() ?? 0.1; // "??" se nao for encontrado, assume 10cm
        }

        private double GetSlabThickness(Element slab)
        {
            return slab.get_Parameter(BuiltInParameter.STRUCTURAL_FLOOR_CORE_THICKNESS)
                ?.AsDouble() ?? 0.4; // "??" se nao for encontrado, assume 40cm
        }
    }
}