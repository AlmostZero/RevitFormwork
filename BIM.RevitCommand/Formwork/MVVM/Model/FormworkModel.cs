using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace BIM.RevitCommand.Formwork.MVVM.Model
{
    public class FormworkModel
    {
        public CategoryModel CategoryModel { get; set; }

        public FormworkHostData HostData { get; set; }

        public Level Level { get; set; }

        public List<Grid> Grids { get; set; }
        public List<Grid> XGrids { get; set; }
        public List<Grid> YGrids { get; set; }

        public String GridX { get; set; }

        public String GridY { get; set; }

        public double FormworkAreaFeet { get; set; }

        public double FormworkAreaMeter { get; set; }

        public double FormworkThk { get; set; }

        public PlanarFace FaceBase { get; set; }

        public XYZ FormworkNormal { get; set; }

        public DirectShape FormworkShape { get; set; }

        public Solid FormworkSolid { get; set; }

    }
}
