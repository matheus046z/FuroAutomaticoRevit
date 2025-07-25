﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FuroAutomaticoRevit.Domain;
using FuroAutomaticoRevit.Revit;
using FuroAutomaticoRevit.UI.Views;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Markup;

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
            RevitLinkInstance structuralLink)
        {
            const string TARGET_VIEW = "Vista teste";
            var results = new List<IntersectionData>();

            //TaskDialog.Show("DEBUG", "Iniciando metodo FindIntersections");

            // Get target view
            View3D targetView = _filterService.GetViewByName(TARGET_VIEW) as View3D;
            if (targetView == null)
            {
                TaskDialog.Show("Erro", $"A vista '{TARGET_VIEW}' não foi encontrada!");
                return results;
            }

            //TaskDialog.Show("Debug", $"Found target view: {targetView.Name}");

            // Initialize spatial filter service
            var spatialFilter = new RevitSpatialFilterService(_doc);

            // Get transforms
            Transform mepTransform = mepLink.GetTotalTransform(); // Retorna norte verdadeiro transform
            Transform structuralTransform = structuralLink.GetTotalTransform();

            // Debug: Transforms
            //TaskDialog.Show("Debug", $"MEP Transform: " +
            //    $"{mepTransform.Origin}\nStructural Transform: {structuralTransform.Origin}");


            // PIPES
            var pipes = GetElementsOfType(
                spatialFilter, mepLink, targetView,
                BuiltInCategory.OST_PipeCurves, SANITARY_PIPE_TYPE);
                //TaskDialog.Show("Debug", $"Found {pipes.Count} pipes of type '{SANITARY_PIPE_TYPE}'");

            //if (pipes.Count == 0)
            //{
            //    //TaskDialog.Show("Debug", "No pipes found. Checking raw elements...");
            //    var rawPipes = spatialFilter.GetElementsInView(mepLink, targetView,
            //        new[] { BuiltInCategory.OST_PipeCurves });
            //    //TaskDialog.Show("Debug", $"Raw pipes found: {rawPipes.Count}");

            //    foreach (var pipe in rawPipes)
            //    {
            //        TaskDialog.Show("Debug", $"Pipe: ID={pipe.Id}, Name={pipe.Name}, " +
            //            $"Type={pipe.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM)?.AsValueString()}");
            //    }
            //}


            var conduits = GetElementsOfType(
                spatialFilter, mepLink, targetView,
                BuiltInCategory.OST_Conduit, ELECTRICAL_CONDUIT_TYPE);
                //TaskDialog.Show("Debug", $"Found {conduits.Count} conduits of type '{ELECTRICAL_CONDUIT_TYPE}'");

            var slabs = GetElementsOfType(
                spatialFilter, structuralLink, targetView,
                BuiltInCategory.OST_Floors, STRUCTURAL_SLAB_TYPE);
                //TaskDialog.Show("Debug", $"Found {slabs.Count} slabs of type '{STRUCTURAL_SLAB_TYPE}'");

            // Combine pipes and conduits
            var allPipes = pipes.Concat(conduits).ToList();
            TaskDialog.Show("Debug", $"Total de canos e conduites: {allPipes.Count}");

            

            // DEBUG
            int pipeCount = 0;
            int slabCount = 0;
            int intersectionCount = 0;
            //

            // Process intersections
            foreach (var pipe in allPipes)
            {
                pipeCount++;
                Solid pipeSolid = GeometryUtils.GetElementSolid(pipe, null);
                if (pipeSolid?.Volume < TOLERANCE)
                {
                    TaskDialog.Show("Debug", $"Pipe {pipe.Id} has no valid solid");
                    continue;
                }

                // Get pipe location in host coordinates
                Transform pipeTransform = mepTransform;
                Solid pipeSolidInHost = GeometryUtils.TransformSolid(pipeSolid, pipeTransform);

                foreach (var slab in slabs)
                {
                    slabCount++;
                    Solid slabSolid = GeometryUtils.GetElementSolid(slab, null);
                    if (slabSolid?.Volume < TOLERANCE)
                    {
                        TaskDialog.Show("Debug", $"Slab {slab.Id} has no valid solid");
                        continue;
                    }

                    // Get slab location in host coordinates
                    Transform slabTransform = structuralTransform;
                    Solid slabSolidInHost = GeometryUtils.TransformSolid(slabSolid, slabTransform);

                    // Get intersection in host coordinates
                    Solid intersection = GeometryUtils.GetIntersectionSolid(
                        pipeSolidInHost,
                        slabSolidInHost
                    );
                    
                    if (intersection?.Volume > TOLERANCE)
                    {
                        intersectionCount++;

                        // Get centroid in host coordinates
                        XYZ centroidInHost = GeometryUtils.GetCentroid(intersection);

                        // Transform centroid back to pipe's coordinate system
                        Transform inversePipeTransform = pipeTransform.Inverse;
                        XYZ centroidInPipeCoords = inversePipeTransform.OfPoint(centroidInHost);

                        // For slabs, we might need structural coordinates instead
                        // Transform centroid to structural coordinates if needed
                        Transform pipeToStructural = slabTransform.Multiply(inversePipeTransform);
                        XYZ centroidInStructuralCoords = pipeToStructural.OfPoint(centroidInPipeCoords);

                        results.Add(new IntersectionData
                        {
                            Pipe = pipe,
                            Slab = slab,
                            Location = centroidInHost, // Use host coordinates for placement
                            PipeDiameter = GetPipeDiameter(pipe),
                            SlabThickness = GetSlabThickness(slab)
                        });

                        // DEBUG: Show coordinate comparisons
                        //TaskDialog.Show("Debug",
                        //    $"Centroid in Host: {centroidInHost}\n" +
                        //    $"Centroid in Pipe Coords: {centroidInPipeCoords}\n" +
                        //    $"Centroid in Structural Coords: {centroidInStructuralCoords}");
                    }
                }
            }
            TaskDialog.Show("Debug", $"Foram processados {pipeCount} tubos. " +
                $"Foram encontrados {intersectionCount} intersecoes");
            return results;
        }


        private List<Element> GetElementsOfType(
            RevitSpatialFilterService spatialFilter,
            RevitLinkInstance link,
            View3D view,
            BuiltInCategory category,
            string typeName)
        {
            var elements = spatialFilter.GetElementsInView(link, view, new[] { category });

            // If no elements found, try without spatial filter
            if (elements.Count == 0)
            {
                TaskDialog.Show("Debug", $"Nenhum elemento foi encontrado com filtro da caixa de corte - Tentando sem");
                Document linkedDoc = link.GetLinkDocument();
                elements = new FilteredElementCollector(linkedDoc)
                    .OfCategory(category)
                    .WhereElementIsNotElementType()
                    .ToList();
            }

            //TaskDialog.Show("Debug", $"Raw elements found for category {category}: {elements.Count}");

            var filtered = elements
                .Where(e => {
                    string name = e.Name ?? "";
                    string typeParam = e.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM)?.AsValueString() ?? "";
                    string categoryName = e.Category?.Name ?? "";

                    bool matches = name.Contains(typeName) ||
                                  typeParam.Contains(typeName) ||
                                  categoryName.Contains(typeName);

                    if (!matches && elements.Count < 20) // Only log if few elements
                    {
                        //TaskDialog.Show("Debug", $"Element filtered out: " +
                        //    $"ID={e.Id}, Name={name}, Type={typeParam}, Category={categoryName}");
                    }

                    return matches;
                })
                .ToList();

            //TaskDialog.Show("Debug", $"Filtered elements for type '{typeName}': {filtered.Count}");

            return filtered;
        }


        //private List<Element> GetElementsOfType(
        //    RevitSpatialFilterService spatialFilter,
        //    RevitLinkInstance link,
        //    View3D view,
        //    BuiltInCategory category,
        //    string typeName)
        //{
        //    var elements = spatialFilter.GetElementsInView(link, view, new[] { category })
        //        .Where(e => e.Name?.Contains(typeName) == true ||
        //               e.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM)
        //                   ?.AsValueString()?.Contains(typeName) == true)
        //        .ToList();

        //    // Debug element types
        //    if (elements.Any())
        //    {
        //        TaskDialog.Show("Debug", $"Elements of type '{typeName}':\n" +
        //            string.Join("\n", elements.Select(e =>
        //                $"ID: {e.Id}, Name: {e.Name}, Type: {e.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM)?.AsValueString()}")));
        //    }
        //    else
        //    {
        //        TaskDialog.Show("Debug", $"No elements found for type '{typeName}'");
        //    }

        //    return elements;
        //}


        //public IList<IntersectionData> FindIntersections(
        //    RevitLinkInstance mepLink,
        //    RevitLinkInstance structuralLink)
        //{
        //    const string TARGET_VIEW = "Vista teste";
        //    var results = new List<IntersectionData>();

        //    // Get target view
        //    View3D targetView = _filterService.GetViewByName(TARGET_VIEW) as View3D;
        //    if (targetView == null)
        //    {
        //        TaskDialog.Show("Erro", $"A vista '{TARGET_VIEW}' não foi encontrada ou não é 3D!");
        //        return results;
        //    }

        //    // Get transforms for coordinate conversion
        //    Transform mepTransform = mepLink.GetTotalTransform();
        //    Transform structuralTransform = structuralLink.GetTotalTransform();

        //    // Initialize spatial filter service
        //    var spatialFilter = new RevitSpatialFilterService(_doc);

        //    // Get elements in view using spatial filter service
        //    var mepCategories = new List<BuiltInCategory> { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_Conduit };
        //    var structuralCategories = new List<BuiltInCategory> { BuiltInCategory.OST_Floors };

        //    var pipes = spatialFilter.GetElementsInView(mepLink, targetView, mepCategories)
        //        .Where(e => e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves)
        //        .ToList();

        //    var conduits = spatialFilter.GetElementsInView(mepLink, targetView, mepCategories)
        //        .Where(e => e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Conduit)
        //        .ToList();

        //    var slabs = spatialFilter.GetElementsInView(structuralLink, targetView, structuralCategories);

        //    // Filter by specific types
        //    pipes = FilterSpecificTypes(pipes, SANITARY_PIPE_TYPE);
        //    conduits = FilterSpecificTypes(conduits, ELECTRICAL_CONDUIT_TYPE);
        //    slabs = FilterSpecificTypes(slabs, STRUCTURAL_SLAB_TYPE);

        //    // Combine pipes and conduits
        //    var allPipes = new List<Element>();
        //    allPipes.AddRange(pipes);
        //    allPipes.AddRange(conduits);

        //    // Process intersections
        //    foreach (var pipe in allPipes)
        //    {
        //        Solid pipeSolid = GeometryUtils.GetElementSolid(pipe, mepTransform);
        //        if (pipeSolid == null || pipeSolid.Volume < TOLERANCE) continue;

        //        foreach (var slab in slabs)
        //        {
        //            Solid slabSolid = GeometryUtils.GetElementSolid(slab, structuralTransform);
        //            if (slabSolid == null || slabSolid.Volume < TOLERANCE) continue;

        //            Solid intersectionSolid = GeometryUtils.GetIntersectionSolid(pipeSolid, slabSolid);

        //            if (intersectionSolid != null && intersectionSolid.Volume > TOLERANCE)
        //            {
        //                results.Add(new IntersectionData
        //                {
        //                    Pipe = pipe,
        //                    Slab = slab,
        //                    Location = GeometryUtils.GetCentroid(intersectionSolid),
        //                    PipeDiameter = GetPipeDiameter(pipe),
        //                    SlabThickness = GetSlabThickness(slab),
        //                    IntersectionSolid = intersectionSolid
        //                });
        //            }
        //        }
        //    }

        //    return results;
        //}

        private double GetPipeDiameter(Element pipe)
        {
            Parameter param = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM) ??
                              pipe.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM);

            return param?.AsDouble() ?? 0.328084; // "??" se nao for encontrado, assume 10cm em pés
        }

        private double GetSlabThickness(Element slab)
        {
                     
            return slab.get_Parameter(BuiltInParameter.STRUCTURAL_FLOOR_CORE_THICKNESS)
                ?.AsDouble() ?? 1.31234; // "??" se nao for encontrado, assume 40cm em pés

            // Adicionar a checagem de outros parâmetros de espessura de laje e piso
            // FLOOR_ATTR_THICKNESS_PARAM
            // Usar UnitUtils.ConvertToInternalUnits para converter para metros ?
            
            // Converter todas as unidades para as unidades do modelo host ?
            //UnitUtils.Convert(
            //data.ElementThickness + 0.10,
            //UnitTypeId.Meters,
            //_doc.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId()

        }
    }
}