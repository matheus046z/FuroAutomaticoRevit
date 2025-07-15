// IntersectionData.cs
using Autodesk.Revit.DB;

namespace FuroAutomaticoRevit.Domain
{
    public class IntersectionData
    {
        public Element Pipe { get; set; }
        public Element Slab { get; set; }
        public Element StructuralElement { get; set; }
        public XYZ Location { get; set; }
        public double PipeDiameter { get; set; }
        public double SlabThickness { get; set; }
        public Solid IntersectionSolid { get; set; }
        public double ElementThickness { get; set; }

        // Calculated hole dimensions

        public double HoleWidth => PipeDiameter * 1.5;
        public double HoleHeight => SlabThickness + 0.10; // 5cm top + 5cm bottom
    }
}