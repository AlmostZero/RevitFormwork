using Autodesk.Revit.DB;
using BIM.RevitCommand.Formwork.MVVM.Model;
using System;
using System.Collections.Generic;

namespace BIM.RevitCommand.Formwork
{
    public class FormworkHostData
    {
        public List<Grid> Grids { get; set; }

        public Level Level { get; set; }

        public String CategoryName { get; set; }

        public String FamilyName { get; set; }

        public BuiltInCategory HostCategory { get; set; }

        public Element HostElement { get; set; }

        public Solid HostSolidUnion { get; set; }

        public List<Solid> HostSolids { get; set; }

        public List<Face> HostFaces { get; set; }

        public List<FormworkHostData> Intersections { get; set; }

        public List<Solid> IntersectionSolids { get; set; }

        public List<FormworkModel> FormworkModels { get; set; }


        public FormworkHostData()
        {
            this.Grids = new List<Grid>();
            this.Level = null;
            this.HostCategory = BuiltInCategory.INVALID;
            this.HostSolidUnion = null;
            this.HostSolids = new List<Solid>();
            this.HostFaces = new List<Face>();
            this.Intersections = new List<FormworkHostData>();
            this.IntersectionSolids = new List<Solid>();
            this.FormworkModels = new List<FormworkModel>();
        }
    }

    public class HostElement
    {
        public Element HostFinal { get; set; }

        public List<Element> SubElements { get; set; }

        public Solid Union { get; set; }

        public List<Solid> Solids { get; set; }
    }

}
