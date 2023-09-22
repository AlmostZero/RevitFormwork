using Autodesk.Revit.DB;
using BIM.RevitCommand.Formwork.Util;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Xml.Serialization;


namespace BIM.RevitCommand.Formwork.MVVM.Model
{
    public class CategoryModel : INotifyPropertyChanged
    {

        #region OnPropertyChanged Method

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged( [CallerMemberName] string propName = "" )
        {
            if ( PropertyChanged != null )
            {
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propName ) );
            }
        }

        #endregion

        [XmlAttribute]
        private bool isSelectedCategory;
        public bool IsSelectedCategory
        {
            get { return isSelectedCategory; }
            set { isSelectedCategory = value; OnPropertyChanged(); }
        }


        [XmlAttribute]
        private String categoryName;
        public String CategoryName
        {
            get { return categoryName; }
            set { categoryName = value; OnPropertyChanged(); }
        }

        [XmlIgnore]
        private System.Drawing.Color color;
        public System.Drawing.Color Color
        {
            get
            {
                color = FormworkUtil.WpfBrushToDrawingColor( formworkColor.Brush );
                return color;
            }
            set { color = value; OnPropertyChanged(); }
        }



        [XmlElement]
        public string ClrGridHtml
        {
            get { return ColorTranslator.ToHtml( color ); }
            set { Color = ColorTranslator.FromHtml( value ); }
        }

        private FormworkColor formworkColor;
        [XmlIgnore]
        public FormworkColor FormworkColor
        {
            get
            {
                if ( formworkColor == null )
                {
                    formworkColor = new FormworkColor()
                    {
                        Brush = new SolidColorBrush( System.Windows.Media.Color.FromRgb( color.R, color.G, color.B ) ),
                        RevitColor = new Autodesk.Revit.DB.Color( color.R, color.G, color.B )
                    };
                }
                return formworkColor;
            }
            set { formworkColor = value; OnPropertyChanged(); }
        }


        private BuiltInCategory builtInCategory;
        [XmlElement]
        public BuiltInCategory BuiltInCategory
        {
            get { return builtInCategory; }
            set { builtInCategory = value; OnPropertyChanged(); }
        }


        private ObservableCollection<Element> elements;
        [XmlIgnore]
        public ObservableCollection<Element> Elements
        {
            get { return elements; }
            set { elements = value; OnPropertyChanged(); }
        }


    }

    public class FormworkColor
    {

        #region OnPropertyChanged Method

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged( [CallerMemberName] string propName = "" )
        {
            if ( PropertyChanged != null )
            {
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propName ) );
            }
        }

        #endregion

        private Autodesk.Revit.DB.Color revitColor;
        public Autodesk.Revit.DB.Color RevitColor
        {
            get { return revitColor; }
            set { revitColor = value; OnPropertyChanged(); }
        }

        private SolidColorBrush brush;
        public SolidColorBrush Brush
        {
            get { return brush; }
            set { brush = value; OnPropertyChanged(); }
        }
    }
}
