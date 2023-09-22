using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using BIM.RevitCommand.Formwork.MVVM.Model;
using System.Xml.Serialization;


namespace BIM.RevitCommand.Formwork.MVVM.ViewModel
{
    [Serializable]
    public class FormworkViewModel : INotifyPropertyChanged
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

        /// <summary>
        /// xmlファイル名(拡張子なし)
        /// </summary>
        private String xmlFileName;
        public String XmlFileName
        {
            get { return xmlFileName; }
            set { xmlFileName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// xml更新時刻
        /// </summary>
        private DateTime savedTime;
        public DateTime SavedTime
        {
            get { return savedTime; }
            set { savedTime = value; OnPropertyChanged(); }
        }

        private ObservableCollection<CategoryModel> categoryModels;
        public ObservableCollection<CategoryModel> CategoryModels
        {
            get { return categoryModels; }
            set { categoryModels = value; OnPropertyChanged(); }
        }

        private double panelThk;
        public double PanelThk
        {
            get { return panelThk; }
            set { panelThk = value; OnPropertyChanged(); }
        }

        private double skipLength;
        public double SkipLength
        {
            get { return skipLength; }
            set { skipLength = value; OnPropertyChanged(); }
        }

        private double skipArea;
        public double SkipArea
        {
            get { return skipArea; }
            set { skipArea = value; OnPropertyChanged(); }
        }

        private double angle;
        public double Angle
        {
            get { return angle; }
            set { angle = value; OnPropertyChanged(); }
        }


        private bool isCreateGroup;
        public bool IsCreateGroup
        {
            get { return isCreateGroup; }
            set { isCreateGroup = value; OnPropertyChanged(); }
        }


        private bool isExportExcel;
        public bool IsExportExcel
        {
            get { return isExportExcel; }
            set { isExportExcel = value; OnPropertyChanged(); }
        }


        private bool isCreateRevitSchedules;
        public bool IsCreateRevitSchedules
        {
            get { return isCreateRevitSchedules; }
            set { isCreateRevitSchedules = value; OnPropertyChanged(); }
        }


        private ObservableCollection<FormworkModel> formworkModels;
        [XmlIgnore]
        public ObservableCollection<FormworkModel> FormworkModels
        {
            get { return formworkModels; }
            set { formworkModels = value; OnPropertyChanged(); }
        }

    }
}
