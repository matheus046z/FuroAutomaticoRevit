using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FuroAutomaticoRevit.Domain;
using FuroAutomaticoRevit.UI.ViewModels;
using System;
using System.Collections.Generic;


namespace FuroAutomaticoRevit.Revit
{
    public class RevitLinkService
    {
        private readonly Document _doc;
        private readonly UIDocument _uiDoc;

        public RevitLinkService(Document doc, UIDocument uiDoc)
        {
            _doc = doc;
            _uiDoc = uiDoc;
        }

        public LinkedModel LinkModel(RvtFile file, string modelType)
        {
            try
            {
                ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(file.FilePath);
                RevitLinkOptions options = new RevitLinkOptions(false);

                using (Transaction t = new Transaction(_doc, $"Vincular {modelType}"))
                {
                    t.Start();

                    LinkLoadResult loadResult = RevitLinkType.Create(_doc, modelPath, options);
                    if (loadResult.LoadResult != LinkLoadResultType.LinkLoaded)
                    {
                        throw new Exception($"Falha no carregamento: {loadResult.LoadResult}");
                    }

                    RevitLinkType linkType = _doc.GetElement(loadResult.ElementId) as RevitLinkType;
                    if (linkType == null) throw new Exception("Tipo de link não encontrado");

                    RevitLinkInstance linkInstance = RevitLinkInstance.Create(_doc, linkType.Id);
                    if (linkInstance == null) throw new Exception("Falha ao criar instância do link");

                    t.Commit();

                    return new LinkedModel
                    {
                        File = file,
                        Instance = linkInstance,
                        Type = linkType,
                        ModelType = modelType
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Falha ao vincular {file.FileName}: {ex.Message}", ex);
            }
        }

        public bool IsFileLinked(string filePath)
        {
            var collector = new FilteredElementCollector(_doc)
                .OfClass(typeof(RevitLinkInstance));

            foreach (RevitLinkInstance instance in collector)
            {
                RevitLinkType type = _doc.GetElement(instance.GetTypeId()) as RevitLinkType;
                if (type == null) continue;

                ModelPath path = type.GetExternalFileReference()?.GetAbsolutePath();
                if (path == null) continue;

                string linkedPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(path);
                if (linkedPath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public void RemoveLinks(IEnumerable<LinkedModel> links)
        {
            using (Transaction t = new Transaction(_doc, "Remover Links"))
            {
                t.Start();
                foreach (var model in links)
                {
                    if (model.Instance.IsValidObject)
                    {
                        _doc.Delete(model.Instance.Id);
                        if (_doc.GetElement(model.Type.Id) != null)
                        {
                            _doc.Delete(model.Type.Id);
                        }
                    }
                }
                t.Commit();
            }
        }
    }
}