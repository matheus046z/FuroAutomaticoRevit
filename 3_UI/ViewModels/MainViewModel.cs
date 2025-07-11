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

        public ObservableCollection<RvtFile> MepModels { get; } = new ObservableCollection<RvtFile>();
        public ObservableCollection<RvtFile> StructuralModels { get; } = new ObservableCollection<RvtFile>();

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
                    //TaskDialog.Show("loadResult:",$"{loadResult}");
                    message += $"LinkLoadResult: {loadResult?.LoadResult.ToString() ?? "null"}\n";

                    if (loadResult == null)
                    {
                        throw new Exception("A criaçao do link retornou resultado nulo");
                    }
                    
                    if (loadResult.LoadResult != LinkLoadResultType.LinkLoaded)
                    {
                        throw new Exception($"O carregamento do link falhou: {loadResult.LoadResult}");
                    }


                    RevitLinkType linkType = doc.GetElement(loadResult.ElementId) as RevitLinkType;
                    //DEBUG
                    //TaskDialog.Show("linkType:",$"{linkType}");
                    message += $"LinkType: {(linkType != null ? "Created" : "Null")}\n";

                    if (linkType == null)
                    {
                        throw new Exception("Falha ao recuperar o tipo do link");
                    }


                    RevitLinkInstance linkInstance = RevitLinkInstance.Create(doc, linkType.Id);
                    //DEBUG
                    message += $"LinkInstance: {(linkInstance != null ? "Created" : "Null")}\n";
                    if (linkInstance == null)
                    {
                        throw new Exception("Falha ao criar instancia do link");
                    }



                    t.Commit(); // Aplica alteraçoes no projeto para testar vinculos


                    /*t.RollBack();*/ // Limpar após o teste de links. Rollback remove o link criado para testar o vínculo.
                                      // (Teoricamente o metodo TestAndLinkFile vai testar os vinculos dando rollback após o teste.
                                      // Criando os vinculos novamente ao pressionar o botao executar.

                    
                    
                    TaskDialog.Show("Sucesso",
                        $"Vínculo criado com sucesso: {file.FileName}");
                }

                catch (Exception ex)
                {
                    t.RollBack();
                    message += $"ERROR: {ex.Message}\n";
                    TaskDialog.Show("Falha ao criar o link",
                        $"Não foi possivel criar link para {file.FileName}:\n{ex.Message}\n\nDebug:\n{message}");
                    return;
                }
            }
        }

        private void Execute(object parameter)
        {
            // Logica principal
            // Serviço de 4_Core e 5_Revit

            // Fechar janela após execução
            (parameter as System.Windows.Window)?.Close();
        }

        private void Cancel(object parameter)
        {
            (parameter as System.Windows.Window)?.Close();
        }
    }
}