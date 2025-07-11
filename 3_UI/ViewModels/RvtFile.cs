using System.IO;

namespace FuroAutomaticoRevit.UI.ViewModels
{
    public class RvtFile
    {
        public string FilePath { get; set; }
        public string FileName => Path.GetFileName(FilePath);
    }
}