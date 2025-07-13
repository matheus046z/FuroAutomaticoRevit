using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FuroAutomaticoRevit.Domain;
using FuroAutomaticoRevit.Revit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

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

            // FUROS


            (parameter as System.Windows.Window)?.Close();
        }

        private void Cancel(object parameter)
        {
            //RemoveCreatedLinks();
            //(parameter as System.Windows.Window)?.Close();

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