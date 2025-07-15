using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using FuroAutomaticoRevit.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FuroAutomaticoRevit.Core
{
    public class HoleCreationService
    {
        private readonly Document _doc;
        private const string FAMILY_NAME = "FURO-QUADRADO-LAJE";
        private const string FAMILY_TYPE = "SDR - Furo na laje";
        private const string HEIGHT_PARAM = "FUR.esp-laje";
        private const string WIDTH_PARAM = "TH-FUR-DIM1";
        private const string LENGTH_PARAM = "TH-FUR-DIM2";

        public HoleCreationService(Document doc)
        {
            _doc = doc;
        }

        public void CreateOpenings(IList<IntersectionData> intersections)
        {
            FamilySymbol symbol = LoadFamilySymbol();
            if (symbol == null)
            {
                TaskDialog.Show("Error", $"Family '{FAMILY_NAME}' or type '{FAMILY_TYPE}' not found!");
                return;
            }

            using (Transaction t = new Transaction(_doc, "Create Slab Openings"))
            {
                t.Start();

                foreach (var data in intersections)
                {
                    CreateOpening(data, symbol);
                }

                t.Commit();
            }
        }

        private void CreateOpening(IntersectionData data, FamilySymbol symbol)
        {
            try
            {
                // Cria instancia na face da laje
                Reference faceRef = GetSlabFaceReference(data.StructuralElement, data.Location);
                if (faceRef == null) return;

                FamilyInstance instance = _doc.Create.NewFamilyInstance(
                    faceRef,
                    data.Location,
                    XYZ.BasisX,
                    symbol
                );

                // Atribui dimnensoes
                SetParameters(instance, data);
            }
            catch
            {
                // Fallback
                try
                {
                    FamilyInstance instance = _doc.Create.NewFamilyInstance(
                        data.Location,
                        symbol,
                        GetLevelAtPoint(data.Location),
                        StructuralType.NonStructural
                    );
                    SetParameters(instance, data);
                }
                catch
                {
                    TaskDialog.Show("Aviso", $"Falha ao criar furo em {data.Location}");
                }
            }
        }

        private void SetParameters(FamilyInstance instance, IntersectionData data)
        {
            // Altura = laje + 10cm
            instance.LookupParameter(HEIGHT_PARAM)?.Set(data.ElementThickness + 0.10);

            // Dimensões da abertura = diametri do tubo * 1.5
            double dimension = data.PipeDiameter * 1.5;
            instance.LookupParameter(WIDTH_PARAM)?.Set(dimension);
            instance.LookupParameter(LENGTH_PARAM)?.Set(dimension);
        }

        private Reference GetSlabFaceReference(Element slab, XYZ point)
        {
            Options options = new Options { ComputeReferences = true };
            GeometryElement geometry = slab.get_Geometry(options);

            if (geometry == null) return null;

            foreach (GeometryObject obj in geometry)
            {
                if (obj is Solid solid)
                {
                    foreach (Face face in solid.Faces)
                    {
                        if (face.Project(point).Distance < 0.001)
                        {
                            return face.Reference;
                        }
                    }
                }
            }
            return null;
        }

        private Level GetLevelAtPoint(XYZ point)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => Math.Abs(l.Elevation - point.Z))
                .FirstOrDefault();
        }

        private FamilySymbol LoadFamilySymbol()
        {
            // Tenta achar o símbolo da família já carregado
            FamilySymbol symbol = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(s => s.FamilyName == FAMILY_NAME && s.Name == FAMILY_TYPE);

            if (symbol != null && symbol.IsActive) return symbol;

            // Carrega a família se não estiver carregada
            string familyPath = FindFamilyPath();
            if (!File.Exists(familyPath)) return null;

            using (Transaction t = new Transaction(_doc, "Load Family"))
            {
                t.Start();
                if (_doc.LoadFamily(familyPath, out Family family))
                {
                    symbol = family.GetFamilySymbolIds()
                        .Select(id => _doc.GetElement(id))
                        .Cast<FamilySymbol>()
                        .FirstOrDefault(s => s.Name == FAMILY_TYPE);

                    if (symbol != null && !symbol.IsActive)
                    {
                        symbol.Activate();
                    }
                }
                t.Commit();
            }
            return symbol;
        }

        private string FindFamilyPath()
        {
            // Procura familia em locais comuns
            string[] paths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autodesk", "Revit 2023", "Libraries", "Brasil", "Specialty Equipment", $"{FAMILY_NAME}.rfa"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Revit", "Families", $"{FAMILY_NAME}.rfa"),
                Path.Combine(Path.GetDirectoryName(_doc.PathName), $"{FAMILY_NAME}.rfa")
            };

            return paths.FirstOrDefault(File.Exists);
        }
    }
}