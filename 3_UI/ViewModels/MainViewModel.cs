using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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

        //private string _selectedMepModel;
        //private string _selectedStructuralModel;


        public ObservableCollection<RvtFile> MepModels { get; } = new ObservableCollection<RvtFile>();
        public ObservableCollection<RvtFile> StructuralModels { get; } = new ObservableCollection<RvtFile>();

        //public string SelectedMepModel
        //{
        //    get => _selectedMepModel;
        //    set => SetField(ref _selectedMepModel, value);
        //}

        //public string SelectedStructuralModel
        //{
        //    get => _selectedStructuralModel;
        //    set => SetField(ref _selectedStructuralModel, value);
        //}

        // In MainViewModel class
        private RvtFile _selectedMepModel;
        public RvtFile SelectedMepModel
        {
            get => _selectedMepModel;
            set
            {
                if (SetField(ref _selectedMepModel, value))
                {
                    TestAndLinkFile(value);
                }
            }
        }

        private RvtFile _selectedStructuralModel;
        public RvtFile SelectedStructuralModel
        {
            get => _selectedStructuralModel;
            set
            {
                if (SetField(ref _selectedStructuralModel, value))
                {
                    TestAndLinkFile(value);
                }
            }
        }


        public ICommand ExecuteCommand { get; }
        public ICommand CancelCommand { get; }

        public MainViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp;


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

            //string pasta = Path.GetDirectoryName(currentDocPath);

            //string[] arquivos = Directory.GetFiles(pasta, "*.rvt");


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
        
            //// Get all linked models in the project
            //var collector = new FilteredElementCollector(_uiApp.ActiveUIDocument.Document);
            //var links = collector.OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>();

            //// Filter MEP models
            //foreach (var link in links.Where(l => l.Name.Contains("MEP")))
            //{
            //    MepModels.Add(link.Name);
            //}

            //// Filter structural models
            //foreach (var link in links.Where(l => l.Name.Contains("Structural")))
            //{
            //    StructuralModels.Add(link.Name);
            //}

            //// Set default selections
            //if (MepModels.Any()) SelectedMepModel = MepModels.First();
            //if (StructuralModels.Any()) SelectedStructuralModel = StructuralModels.First();
        }

        private void TestAndLinkFile(RvtFile file)
        {
            if (file == null) return;

            Document doc = _uiApp.ActiveUIDocument.Document;
            string message = "";
            //bool success = false;


            using (Transaction t = new Transaction(doc, "Test Link"))
            {
                try
                {
                    t.Start();

                    ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(file.FilePath);

                    RevitLinkOptions options = new RevitLinkOptions(false);

                    LinkLoadResult loadResult = RevitLinkType.Create(doc, modelPath, options);

                    // DEBUG
                    
                    //message += $"LinkLoadResult: {loadResult?.LoadResult.ToString() ?? "null"}\n";

                    if (loadResult == null)
                    {
                        throw new Exception("A criaçao do link retornou resultado nulo");
                    }
 

                    if (loadResult.LoadResult != LinkLoadResultType.LinkLoaded)
                    {
                        throw new Exception($"Link loading failed: {loadResult.LoadResult}");
                    }


                    RevitLinkType linkType = doc.GetElement(loadResult.ElementId) as RevitLinkType;

                    //message += $"LinkType: {(linkType != null ? "Created" : "Null")}\n";

                    if (linkType == null)
                    {
                        throw new Exception("Falha ao recuperar o tipo do link");
                    }



                    // RevitLinkInstance NAO CRIA LINKS ANINHADOS NESTED
                    RevitLinkInstance linkInstance = RevitLinkInstance.Create(doc, linkType.Id);
                    //message += $"LinkInstance: {(linkInstance != null ? "Created" : "Null")}\n";






                    if (linkInstance == null)
                    {
                        throw new Exception("Failed to create link instance");
                    }


                    t.RollBack();

                    //message += "Transaction rolled back successfully\n";
                    //success = true;


                    //RevitLinkType linkType = RevitLinkType.Create(doc, file.FilePath);
                    //RevitLinkInstance.Create(doc, linkType.Id);


                    /*t.RollBack();*/ // Cleanup after test

                    //TaskDialog.Show("Sucesso",
                    //    $"Vínculo criado com sucesso: {file.FileName}");
                }

                catch (Exception ex)
                {
                    t.RollBack();
                    message += $"ERROR: {ex.Message}\n";
                    TaskDialog.Show("Linking Failed",
                        $"Couldn't link {file.FileName}:\n{ex.Message}\n\nDebug Info:\n{message}");
                    return;
                }
            }
        }



        private void Execute(object parameter)
        {
            // Main execution logic will go here
            // This will call services from 4_Core and 5_Revit

            // Close the window after execution
            (parameter as System.Windows.Window)?.Close();
        }

        private void Cancel(object parameter)
        {
            (parameter as System.Windows.Window)?.Close();
        }
    }
}