using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.UI;
using BIM.RevitCommand.Formwork.Util;
using BIM.RevitCommand.Formwork.MVVM.Model;
using BIM.RevitCommand.Formwork.MVVM.View;
using BIM.RevitCommand.Formwork.MVVM.ViewModel;

namespace BIM.RevitCommand.Formwork.Handler
{
    public class RequestHandler : IExternalEventHandler
    {
        private FormworkViewModel viewModel;

        private UIApplication uiapp;
        private UIDocument uidoc;
        private Document doc;

        private List<Level> projectLevels;
        private List<Grid> projectGrids;
        private DirectShapeType dsType;
        private ElementId fillPatternId;

        public MainView MainView { get; set; }

        private Request request = new Request();
        public Request Request
        {
            get { return request; }
        }

        private void SetViewModel()
        {
            this.viewModel = this.MainView.ViewModel;
            if ( viewModel != null )
            {
                viewModel.FormworkModels = new ObservableCollection<FormworkModel>();
            }
        }


        public void Execute( UIApplication uiapp )
        {
            this.uiapp = uiapp;
            this.uidoc = uiapp.ActiveUIDocument;
            this.doc = this.uidoc.Document;

            try
            {
                switch ( Request.Take() )
                {
                    case RequestId.CreateFormwork:
                    {
                        this.projectLevels = FormworkUtil.Get_ProjectLevels( doc );
                        this.projectGrids = FormworkUtil.Get_Project_Grids( doc );
                        this.fillPatternId = FormworkUtil.GetSolidPatternId( doc );

                        SolidOptions solidOption = new SolidOptions( ElementId.InvalidElementId,
                                             ElementId.InvalidElementId );

                        MainView.richTxtbox.Document.Blocks.Clear();
                        SetViewModel();

                        Options options = new Options()
                        {
                            ComputeReferences = true,
                            IncludeNonVisibleObjects = true,
                        };

                        List<ElementId> selectedIds = uidoc.Selection.GetElementIds()
                                                                     .ToList();

                        if ( selectedIds.Count > 0 )
                        {
                            List<FormworkHostData> dataHosts = Get_Formwork_HostData( selectedIds );

                            MainView.Run( MainView.progressBar1, dataHosts, ( dataHost ) =>
                            {
                                if ( dataHost.HostSolidUnion != null )
                                {
                                    ElementId hostElementId = dataHost.HostElement.Id;

                                    bool flag = false;

                                    foreach ( FormworkHostData dataCompared in dataHosts )
                                    {
                                        ElementId idCompared = dataCompared.HostElement.Id;

                                        if ( hostElementId.IntegerValue.Equals( idCompared.IntegerValue ) )
                                            continue;

                                        foreach ( Solid solidCompared in dataCompared.HostSolids )
                                        {
                                            if ( FormworkUtil.IsSolidIntersect( dataHost.HostSolidUnion, solidCompared ) )
                                            {
                                                dataHost.IntersectionSolids.Add( solidCompared );
                                                dataHost.Intersections.Add( dataCompared );
                                                flag = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            } );


                            using ( TransactionGroup tGroup = new TransactionGroup( doc, "Create Formwork" ) )
                            {
                                tGroup.Start();

                                #region Transaction : Set up shared parameters

                                using ( Transaction t = new Transaction( doc, "t" ) )
                                {
                                    try
                                    {
                                        t.Start();
                                        FormworkParamUtil.CreateSharedParameter( doc );
                                        t.Commit();
                                    }
                                    catch ( Exception ex )
                                    {
                                        MainView.richTxtbox.AppendText( ex.Message + Environment.NewLine );
                                        tGroup.RollBack();
                                    }
                                }

                                #endregion

                                #region Transaction : Set DS type

                                using ( Transaction t = new Transaction( doc, "t" ) )
                                {
                                    try
                                    {
                                        t.Start();
                                        this.dsType = Set_DirectShateType();
                                        t.Commit();
                                    }
                                    catch ( Exception ex )
                                    {
                                        MainView.richTxtbox.AppendText( ex.Message + Environment.NewLine );
                                        tGroup.RollBack();
                                    }
                                }

                                #endregion

                                #region Transaction : Create Formwork shapes

                                using ( Transaction t = new Transaction( doc, "t" ) )
                                {
                                    try
                                    {
                                        t.Start();

                                        MainView.Run( MainView.progressBar2, dataHosts, ( dataHost ) =>
                                        {
                                            dataHost.FormworkModels = CreateFormwork_FaceSeparation( dataHost,
                                                                                                     this.dsType,
                                                                                                     solidOption );
                                        } );

                                        t.Commit();
                                    }
                                    catch ( Exception ex )
                                    {
                                        MainView.richTxtbox.AppendText( ex.Message + Environment.NewLine );
                                        t.RollBack();
                                    }
                                }

                                #endregion

                                #region Transaction : Setup Parameters

                                using ( Transaction t = new Transaction( doc, "t" ) )
                                {
                                    try
                                    {
                                        t.Start();

                                        double formworkAreaTotal = 0.0;

                                        MainView.Run( MainView.progressBar3, dataHosts, ( dataHost ) =>
                                        {
                                            MainView.Run( MainView.progressBar4, dataHost.FormworkModels, ( formwork ) =>
                                            {
                                                formworkAreaTotal += Set_FormworkParameter( dataHost, formwork );
                                            } );
                                        } );

                                        string log = $"[面積合計 : {formworkAreaTotal} ㎡]";
                                        MainView.richTxtbox.AppendText( log );


                                        t.Commit();
                                    }
                                    catch ( Exception ex )
                                    {
                                        MainView.richTxtbox.AppendText( ex.Message + Environment.NewLine );
                                        t.RollBack();
                                    }
                                }

                                #endregion

                                #region Transaction : Formworks Grouping

                                using ( Transaction t = new Transaction( doc, "t" ) )
                                {
                                    try
                                    {
                                        List<FormworkModel> formworkModels = new List<FormworkModel>();

                                        foreach ( FormworkHostData hostData in dataHosts )
                                        {
                                            if ( hostData.FormworkModels.Count > 0 )
                                                formworkModels.AddRange( hostData.FormworkModels );
                                        }

                                        if ( formworkModels.Count < 2 )
                                            return;

                                        var id = formworkModels.Select( x => x.FormworkShape.Id )
                                                               .Cast<ElementId>()
                                                               .ToList();

                                        if ( this.viewModel.IsCreateGroup )
                                        {
                                            t.Start();

                                            Group group = doc.Create.NewGroup( id );
                                            if ( group != null )
                                            {
                                                //string data = DateTime.Now.ToString( "HH:mm:ss" );
                                                group.GroupType.Name = "型枠モデル";
                                            }

                                            t.Commit();
                                        }

                                    }
                                    catch ( Exception ex )
                                    {
                                        MainView.richTxtbox.AppendText( ex.Message + Environment.NewLine );
                                        if ( t.HasStarted() )
                                            t.RollBack();
                                    }
                                }

                                #endregion

                                #region Transaction : Formwork Schedule Creation

                                if ( viewModel.IsCreateRevitSchedules )
                                {
                                    ViewSchedule viewSchedule = null;
                                    using ( Transaction t = new Transaction( doc, "Create Formwork Schedule" ) )
                                    {
                                        t.Start();

                                        try
                                        {
                                            viewSchedule = ScheduleUtil.Create_Formwork_Schedule( doc,
                                                                                                  "型枠集計表",
                                                                                                  "JBK_FACE_DS" );
                                        }
                                        catch ( Exception )
                                        {
                                            if ( t.HasStarted() )
                                                t.RollBack();
                                        }

                                        t.Commit();
                                    }

                                    if ( viewSchedule != null )
                                    {
                                        uidoc.ActiveView = viewSchedule;
                                    }
                                }

                                #endregion

                                #region Excel Exporter

                                if ( viewModel.IsExportExcel )
                                {
                                    //未完成
                                    //ExcelUtil.ExportExcel( MainView.dgFormwork );
                                }

                                #endregion

                                tGroup.Assimilate();
                            }
                        }
                        else
                        {
                            MainView.richTxtbox.AppendText( $"選択中のElementがありません。{Environment.NewLine}" );
                        }
                        break;
                    }
                    case RequestId.DeleteElement:
                    {
                        using ( Transaction t = new Transaction( doc, "Delete Foromwork" ) )
                        {
                            try
                            {
                                if ( MainView.dgFormwork.SelectedItems.Count > 0 )
                                {
                                    List<FormworkModel> delectModels = new List<FormworkModel>();

                                    foreach ( FormworkModel formwork in MainView.dgFormwork.SelectedItems )
                                    {
                                        delectModels.Add( formwork );
                                    }

                                    if ( delectModels.Count > 0 )
                                    {
                                        t.Start();
                                        doc.Delete( delectModels.Select( x => x.FormworkShape.Id ).ToList() );
                                        t.Commit();
                                    }
                                }
                            }
                            catch ( Exception )
                            {
                                if ( t.HasStarted() )
                                    t.RollBack();
                                return;
                            }
                        }
                        break;
                    }
                    case RequestId.None:
                    {
                        break;
                    }
                }
            }
            catch ( Exception ex )
            {
                MainView.richTxtbox.AppendText( $"{FormworkUtil.Get_CodeLineNumber( ex )} : {ex.Message + Environment.NewLine}" );
                return;
            }
        }

        public string GetName() { return "Modeless Form"; }



        private List<FormworkHostData> Get_Formwork_HostData( List<ElementId> selectedIds )
        {
            List<FormworkHostData> hostData = new List<FormworkHostData>();

            List<HostElement> groupByHost = FormworkUtil.GroupElementsByHost( doc, selectedIds );

            foreach ( HostElement hostElement in groupByHost )
            {
                BuiltInCategory hostCat = ( BuiltInCategory )hostElement.HostFinal.Category.Id.IntegerValue;

                if ( hostElement.Union != null )
                {
                    List<Face> hostFaces = new List<Face>();

                    foreach ( Face face in hostElement.Union.Faces )
                    {
                        if ( face.Area > 0.0 && face is PlanarFace )
                        {
                            hostFaces.Add( face );
                        }
                    }

                    string familyName = null;
                    var typeId = hostElement.HostFinal.GetTypeId();
                    if ( typeId != null )
                    {
                        var elementType = doc.GetElement( typeId ) as ElementType;
                        familyName = elementType?.FamilyName;
                    }

                    Level level = FormworkUtil.FindLevelParameterByCategory( doc, hostElement.HostFinal, this.projectLevels );
                    if ( level == null )
                    {
                        level = FormworkUtil.Get_LevelByCoord( hostElement.Union, this.projectLevels );
                    }

                    FormworkHostData hostDate = new FormworkHostData()
                    {
                        Grids = FormworkUtil.Get_Grid_HostElementAround( hostElement.HostFinal, doc.ActiveView, this.projectGrids ),
                        Level = level,
                        CategoryName = hostElement.HostFinal.Category.Name,
                        FamilyName = familyName,
                        HostCategory = hostCat,
                        HostElement = hostElement.HostFinal,
                        HostSolidUnion = hostElement.Union,
                        HostSolids = hostElement.Solids,
                        HostFaces = hostFaces,
                        //Intersections = new List<FormworkHostData>(),
                        //IntersectionSolids = new List<Solid>(),
                        //FormworkModels = new List<FormworkModel>(),
                    };

                    hostData.Add( hostDate );
                }
            }

            return hostData;
        }

        [Obsolete]
        private List<FormworkHostData> Get_Formwork_HostData( List<ElementId> selectedIds,
                                                              Options options )
        {
            List<FormworkHostData> hostData = new List<FormworkHostData>();

            // ここから修正　List<HostElement> groupByHost　→　List<FormworkHostData>
            List<HostElement> groupByHost = FormworkUtil.GroupElementsByHost( doc, selectedIds );

            // REVITの選択されたすべてのElementに対するロープ
            // カテゴリーフィルターなし
            foreach ( ElementId selectedId in selectedIds )
            {
                Element elemSelected = doc.GetElement( selectedId );
                BuiltInCategory hostCat = ( BuiltInCategory )elemSelected.Category.Id.IntegerValue;


                List<Solid> selectedSolids = FormworkUtil.GetElementSolids( elemSelected, options );

                // REVITの選択された１つのElementを１つのSolidとする。
                if ( selectedSolids.Count > 0 )
                {
                    Solid union = null;

                    List<Face> hostFaces = new List<Face>();

                    if ( selectedSolids.Count > 1 )
                    {
                        union = FormworkUtil.Create_UnionSolid( selectedSolids );
                    }
                    else
                    {
                        union = selectedSolids.FirstOrDefault();
                    }

                    if ( union != null )
                    {
                        foreach ( Face face in union.Faces )
                        {
                            if ( face.Area > 0.0 && face is PlanarFace )
                            {
                                hostFaces.Add( face );
                            }
                        }
                    }

                    FormworkHostData hostDate = new FormworkHostData()
                    {
                        Grids = FormworkUtil.Get_Grid_HostElementAround( elemSelected, doc.ActiveView, this.projectGrids ),
                        HostCategory = hostCat,
                        HostElement = elemSelected,
                        HostSolidUnion = union,
                        HostSolids = selectedSolids,
                        HostFaces = hostFaces,
                        Intersections = new List<FormworkHostData>(),
                        IntersectionSolids = new List<Solid>(),
                        FormworkModels = new List<FormworkModel>(),
                    };

                    hostData.Add( hostDate );
                }

            }

            return hostData;
        }

        private List<FormworkModel> CreateFormwork_FaceSeparation( FormworkHostData hostData,
                                                                   DirectShapeType dsType,
                                                                   SolidOptions solidOption )
        {
            List<FormworkModel> formworkModels = new List<FormworkModel>();

            Solid hostSolid = hostData.HostSolidUnion;
            Solid interUnion = null;

            List<Solid> interSolids = new List<Solid>();


            if ( hostData.Intersections.Count > 0 )
            {
                foreach ( FormworkHostData interData in hostData.Intersections )
                {
                    Solid interSolid = interData.HostSolidUnion;
                    if ( interSolid == null )
                        continue;

                    interSolids.Add( interSolid );
                }

                interUnion = FormworkUtil.Create_UnionSolid( interSolids );
            }

            Element hostElem = hostData.HostElement;

            // カテゴリー分類
            var catModels = MainView.ViewModel.CategoryModels.Where( x => x.IsSelectedCategory ).ToList();
            BuiltInCategory hostCat = ( BuiltInCategory )hostElem.Category.Id.IntegerValue;
            if ( !catModels.Exists( x => x.BuiltInCategory.Equals( hostCat ) ) )
                return formworkModels;

            double panelThk = UnitUtil.Millimeters_To_Feet( MainView.ViewModel.PanelThk );
            double skipLength = UnitUtil.Millimeters_To_Feet( MainView.ViewModel.SkipLength );
            double skipArea = UnitUtil.Area_Meter_To_Area_Feet( MainView.ViewModel.SkipArea );

            foreach ( Face face in hostSolid.Faces )
            {
                PlanarFace pf = face as PlanarFace;

                if ( pf == null )
                    continue;

                IList<CurveLoop> curveLoops = pf.GetEdgesAsCurveLoops();
                bool skip = false;
                foreach ( CurveLoop curveLoop in curveLoops )
                {
                    int skipLengthCount = curveLoop.Where( x => x.Length <= skipLength ).ToList().Count;
                    int loopCount = curveLoop.ToList().Count;

                    if ( face.Area <= skipArea )
                    {
                        skip = true;
                        break;
                    }
                }
                if ( skip )
                    continue;



                if ( pf.Area > 0.0 && FormworkUtil.IsAngleOver( pf, 30 ) )
                {
                    IList<GeometryObject> dsObj = new List<GeometryObject>();

                    try
                    {

                        XYZ formworkNormal = pf.FaceNormal;

                        Solid resultSolid = null;

                        IList<CurveLoop> loops = FormworkUtil.OptimizeCurveLoops( pf, uiapp.Application.ShortCurveTolerance );

                        if ( loops.Count < 1 )
                            continue;

                        Solid solidFormwork = null;

                        try
                        {
                            double loopArea = ExporterIFCUtils.ComputeAreaOfCurveLoops( curveLoops );
                            if ( loopArea > 0 )
                            {
                                solidFormwork = GeometryCreationUtilities.CreateExtrusionGeometry( loops,
                                                                                                   pf.FaceNormal,
                                                                                                   panelThk,
                                                                                                   solidOption );
                            }
                        }
                        catch ( Exception ex )
                        {
                            //MainView.richTxtbox.AppendText(
                            //                           $"{FormworkUtil.Get_CodeLineNumber( ex )} : " +
                            //                           $"{loops.Count}(HostID : {hostData.HostElement.Id.IntegerValue})_" +
                            //                           $" {ex.Message + Environment.NewLine}" );
                            continue;
                        }


                        if ( solidFormwork == null )
                            continue;

                        if ( interUnion != null && solidFormwork.Volume > 0.001 )
                        {
                            Solid dif = null;
                            try
                            {
                                dif = BooleanOperationsUtils.ExecuteBooleanOperation( solidFormwork,
                                                                                      interUnion,
                                                                                      BooleanOperationsType.Difference );
                            }
                            catch
                            {
                                dif = solidFormwork;
                            }

                            if ( dif != null )
                            {
                                if ( dif.Volume > 0.001 )
                                {
                                    resultSolid = dif;
                                    dsObj.Add( resultSolid );
                                }
                            }
                        }
                        else
                        {
                            if ( solidFormwork.Volume > 0.001 && solidFormwork.Faces.Size >= 6 )
                            {
                                resultSolid = solidFormwork;
                                dsObj.Add( resultSolid );
                            }
                        }


                        DirectShape ds = DirectShape.CreateElement( doc,
                                                                    new ElementId( BuiltInCategory.OST_GenericModel ) );
                        if ( ds != null && resultSolid != null )
                        {
                            ds.SetTypeId( dsType.Id );
                            ds.SetName( "JBK_FACE_DS" );
                            ds.SetShape( dsObj ); // 生成

                            CategoryModel catModel = MainView.ViewModel.CategoryModels
                                                     .Where( x => x.BuiltInCategory.Equals( hostCat ) )
                                                     .FirstOrDefault();

                            FormworkUtil.OverrideDisplayElement( doc, ds, catModel.FormworkColor.RevitColor, fillPatternId );

                            double areaFeet = Get_ShapeArea( resultSolid, formworkNormal );
                            double areaMeter = Math.Round( UnitUtil.Area_Feet_To_Area_Meter( areaFeet ), 3 );

                            var xGrids = FormworkUtil.FindGridByDirection( XY.X, hostData.Grids, 10 );
                            var yGrids = FormworkUtil.FindGridByDirection( XY.Y, hostData.Grids, 10 );

                            FormworkModel formwork = new FormworkModel()
                            {
                                CategoryModel = catModel,
                                HostData = hostData,
                                Level = hostData.Level,
                                Grids = hostData.Grids,
                                GridX = xGrids.FirstOrDefault()?.Name,
                                GridY = yGrids.FirstOrDefault()?.Name,
                                FormworkAreaFeet = areaFeet,
                                FormworkAreaMeter = areaMeter,
                                FormworkThk = panelThk,
                                FaceBase = pf,
                                FormworkNormal = formworkNormal,
                                FormworkShape = ds,
                                FormworkSolid = resultSolid,
                            };

                            this.MainView.ViewModel.FormworkModels.Add( formwork );

                            formworkModels.Add( formwork );
                        }

                    }
                    catch ( Exception ex )
                    {
                        MainView.richTxtbox.AppendText(
                            $"{FormworkUtil.Get_CodeLineNumber( ex )} : " +
                            $"{hostData.HostElement.Name}(ID : {hostData.HostElement.Id.IntegerValue})_" +
                            $" {ex.Message + Environment.NewLine}" );
                        continue;
                    }
                }
            }

            return formworkModels;
        }


        private double Set_FormworkParameter( FormworkHostData hostData,
                                              FormworkModel formwork )
        {
            double totalArea = 0.0;
            try
            {
                Element hostElement = hostData.HostElement;

                DirectShape shape = formwork.FormworkShape;

                shape.LookupParameter( FormworkParamUtil.GetName( FormworkParameter.Level ) ).Set( formwork.Level?.Name );
                shape.LookupParameter( FormworkParamUtil.GetName( FormworkParameter.GridX ) ).Set( formwork.GridX );
                shape.LookupParameter( FormworkParamUtil.GetName( FormworkParameter.GridY ) ).Set( formwork.GridY );
                shape.LookupParameter( FormworkParamUtil.GetName( FormworkParameter.Category ) ).Set( hostData.CategoryName );
                shape.LookupParameter( FormworkParamUtil.GetName( FormworkParameter.Family ) ).Set( hostData.FamilyName );
                shape.LookupParameter( FormworkParamUtil.GetName( FormworkParameter.FamilyType ) ).Set( hostElement.Name );
                shape.LookupParameter( FormworkParamUtil.GetName( FormworkParameter.Area ) ).Set( formwork.FormworkAreaFeet );
                shape.LookupParameter( FormworkParamUtil.GetName( FormworkParameter.HostId ) ).Set( hostElement.Id.IntegerValue );

                totalArea += formwork.FormworkAreaMeter;
            }
            catch ( Exception ex )
            {
                MainView.richTxtbox.AppendText(
                    $"{FormworkUtil.Get_CodeLineNumber( ex )} : " +
                    $"{hostData.HostElement.Name}(ID : {hostData.HostElement.Id.IntegerValue})_" +
                    $" {ex.Message + Environment.NewLine}" );
            }

            return totalArea;
        }

        private double Get_ShapeArea( Solid formworkSolid,
                                      XYZ normal )
        {
            double area = 0.0;

            if ( formworkSolid != null )
            {
                foreach ( Face f in formworkSolid.Faces )
                {
                    PlanarFace pf = f as PlanarFace;
                    if ( pf != null )
                    {
                        if ( pf.Area > 0.0 )
                        {
                            bool isZero = pf.FaceNormal.CrossProduct( normal ).IsZeroLength();
                            double dot = pf.FaceNormal.DotProduct( normal );

                            double test = UnitUtil.Area_Feet_To_Area_Meter( pf.Area );

                            if ( FormworkUtil.IsEqual( dot, 1 ) )
                            {
                                area += pf.Area;
                            }
                        }
                    }
                }
            }

            return area;
        }

        private DirectShapeType Set_DirectShateType()
        {
            DirectShapeType dsType = null;
            using ( var collector = new FilteredElementCollector( doc ) )
            {
                dsType = collector.OfClass( typeof( DirectShapeType ) )
                                  .OfCategory( BuiltInCategory.OST_GenericModel )
                                  .Cast<DirectShapeType>()
                                  .Where( x => x.Name.Equals( "JBK_FACS_DS" ) )
                                  .FirstOrDefault();

                if ( dsType == null )
                {
                    ElementId categoryId = new ElementId( BuiltInCategory.OST_GenericModel );
                    dsType = DirectShapeType.Create( doc, "JBK_FACS_DS", categoryId );
                }
            }
            return dsType;
        }



    }
}
