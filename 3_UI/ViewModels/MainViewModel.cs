using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FuroAutomaticoRevit.Core;
using FuroAutomaticoRevit.Domain;
using FuroAutomaticoRevit.Revit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace FuroAutomaticoRevit.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly UIApplication _uiApp;
        private readonly RevitLinkService _linkService;
        private readonly LinkVisibilityService _visibilityService;
        private readonly List<LinkedModel> _createdLinks = new List<LinkedModel>();

        public ObservableCollection<RvtFile> MepModels { get; } = new ObservableCollection<RvtFile>();
        public ObservableCollection<RvtFile> StructuralModels { get; } = new ObservableCollection<RvtFile>();

        private RvtFile _selectedMepModel;
        public RvtFile SelectedMepModel
        {
            get => _selectedMepModel;
            set
            {
                if (SetField(ref _selectedMepModel, value) && value != null)
                {
                    LinkModel(value, "MEP");
                }
            }
        }

        private RvtFile _selectedStructuralModel;

        public RvtFile SelectedStructuralModel
        {
            get => _selectedStructuralModel;
            set
            {
                if (SetField(ref _selectedStructuralModel, value) && value != null)
                {
                    LinkModel(value, "Structural");
                }
            }
        }

        public ICommand ExecuteCommand { get; }
        public ICommand CancelCommand { get; }

        public MainViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp;

            // Iniciando serviços
            _linkService = new RevitLinkService(
               uiApp.ActiveUIDocument.Document,
               uiApp.ActiveUIDocument);

            _visibilityService = new LinkVisibilityService(
                uiApp.ActiveUIDocument.Document,
                uiApp.ActiveUIDocument);


            ExecuteCommand = new RelayCommand(Execute);
            CancelCommand = new RelayCommand(Cancel);

            LoadAvailableModels();
        }

        private void LoadAvailableModels()
        {

            Document doc = _uiApp.ActiveUIDocument.Document;
            string currentDocPath = doc.PathName;

            if (string.IsNullOrEmpty(currentDocPath))
            {
                TaskDialog.Show("Erro", "Primeiro salve o projeto na mesma pasta dos arquivos!");
                return;
            }

            string directory = Path.GetDirectoryName(currentDocPath);

            var rvtFiles = Directory.GetFiles(directory, "*.rvt")
                .Where(f => !f.Equals(currentDocPath, StringComparison.OrdinalIgnoreCase))
                .Select(f => new RvtFile { FilePath = f });


            MepModels.Clear();
            StructuralModels.Clear();


            foreach (var file in rvtFiles)
            {
                MepModels.Add(file);
                StructuralModels.Add(file);
            }

        }

        private void LinkModel(RvtFile file, string modelType)
        {
            try
            {
                // Seleção de arquivos
                if (modelType == "MEP")
                {
                    _selectedMepModel = file;
                    OnPropertyChanged(nameof(SelectedMepModel));
                }
                else
                {
                    _selectedStructuralModel = file;
                    OnPropertyChanged(nameof(SelectedStructuralModel));
                }

                // Não linkar se ja existir vínculo
                if (_createdLinks.Any(l => l.File.FilePath == file.FilePath))
                {
                    //TaskDialog.Show("Info", $"O modelo {file.FileName} já está vinculado");
                    return;
                }

                // Pular criação de vinvulo se ja existir
                if (_linkService.IsFileLinked(file.FilePath))
                {
                    //TaskDialog.Show("Info", $"O modelo {file.FileName} já está vinculado no projeto");
                    return;
                }
         
                LinkedModel linkedModel = _linkService.LinkModel(file, modelType);
                _visibilityService.EnsureLinkVisibility(linkedModel.Instance);
                _createdLinks.Add(linkedModel);

                TaskDialog.Show("Sucesso", $"Modelo {file.FileName} vinculado com sucesso!");

            }
            catch (Exception ex)
            {
                TaskDialog.Show("Erro de Vinculação", ex.Message);
            }
        }

        private void Execute(object parameter)
        {
            try
            {
                Document doc = _uiApp.ActiveUIDocument.Document;

                // TESTANDO SELEÇÃO DA VISTA TESTE - VERIFICAR SE O VIEW BOX ESTA NELA (VIEW BOX != CROPBOX)
                const string TARGET_VIEW_NAME = "Vista teste";
                View3D targetView = new FilteredElementCollector(doc)
                    .OfClass(typeof(View3D))
                    .Cast<View3D>()
                    .FirstOrDefault(v => v.Name.Equals(TARGET_VIEW_NAME));

                if (targetView == null)
                {
                    TaskDialog.Show("Erro", $"A vista '{TARGET_VIEW_NAME}' não foi encontrada!");
                    return;
                }

                TaskDialog.Show("Debug", $"Target view found: {targetView.Name}");
                TaskDialog.Show("Debug", $"Section box active: {targetView.IsSectionBoxActive}");

                // Verify section box is active
                if (!targetView.IsSectionBoxActive)
                {
                    TaskDialog.Show("Erro", "A 'Section Box' não está ativa na vista 'Vista teste'. Ative-a e tente novamente.");
                    return;
                }

                // Get and log section box details
                BoundingBoxXYZ sectionBox = targetView.GetSectionBox();
                if (sectionBox == null)
                {
                    TaskDialog.Show("Erro", "Section box não encontrada na vista!");
                    return;
                }

                TaskDialog.Show("Debug", $"Section box: Min={sectionBox.Min}, Max={sectionBox.Max}");


                // Get links
                var mepLink = GetLinkInstance(SelectedMepModel, doc);
                var structuralLink = GetLinkInstance(SelectedStructuralModel, doc);

                if (mepLink == null || structuralLink == null)
                {
                    TaskDialog.Show("Erro", "Selecione ambos os modelos!");
                    return;
                }

                // Find intersections
                var intersectionService = new IntersectionService(doc);
                var intersections = intersectionService.FindIntersections(mepLink, structuralLink);

                if (intersections.Count == 0)
                {
                    TaskDialog.Show("Info", "Nenhuma interseção encontrada");
                    return;
                }

                // Debug coordinate systems
                ProjectPosition projectPosition = doc.ActiveProjectLocation.GetProjectPosition(XYZ.Zero);
                TaskDialog.Show("Debug", $"Host Project Position: " +
                    $"EastWest={projectPosition.EastWest}, " +
                    $"NorthSouth={projectPosition.NorthSouth}, " +
                    $"Elevation={projectPosition.Elevation}");

                if (mepLink != null)
                {
                    Document linkedDoc = mepLink.GetLinkDocument();
                    ProjectPosition linkedPosition = linkedDoc.ActiveProjectLocation.GetProjectPosition(XYZ.Zero);
                    TaskDialog.Show("Debug", $"MEP Link Project Position: " +
                        $"EastWest={linkedPosition.EastWest}, " +
                        $"NorthSouth={linkedPosition.NorthSouth}, " +
                        $"Elevation={linkedPosition.Elevation}");
                }


                TaskDialog.Show("Debug", $"Creating {intersections.Count} openings");
                new HoleCreationService(doc).CreateOpenings(intersections);
                TaskDialog.Show("Sucesso", $"Criadas {intersections.Count} aberturas");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Erro", $"Exception: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                (parameter as Window)?.Close();
            }
        }

        private RevitLinkInstance GetLinkInstance(RvtFile file, Document doc)
        {
            if (file == null) return null;

            return new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .FirstOrDefault(li => {
                    RevitLinkType type = doc.GetElement(li.GetTypeId()) as RevitLinkType;
                    if (type == null) return false;

                    ModelPath path = type.GetExternalFileReference()?.GetAbsolutePath();
                    if (path == null) return false;

                    string linkedPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(path);
                    return linkedPath.Equals(file.FilePath, StringComparison.OrdinalIgnoreCase);
                });
        }

        private void Cancel(object parameter)
        {

            try
            {
                if (_createdLinks.Any())
                {
                    _linkService.RemoveLinks(_createdLinks);
                    TaskDialog.Show("Info", "Vinculos temporários removidos com sucesso");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Erro", $"Falha ao remover links: {ex.Message}");
            }
            finally
            {
                (parameter as System.Windows.Window)?.Close();
            }

        }
    }
}