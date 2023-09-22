using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;


namespace BIM.RevitCommand.Formwork.Util
{
    internal static class FormworkUtil
    {
        public const double _eps = 1.0e-9;

        public static bool IsZero( double a, double tolerance = _eps )
        {
            return tolerance > Math.Abs( a );
        }

        public static bool IsEqual( double a, double b, double tolerance = _eps )
        {
            return IsZero( b - a, tolerance );
        }

        public static bool IsLessOrEqual( double a, double b, double tolerance = _eps )
        {
            return IsEqual( a, b, tolerance ) || a < b;
        }

        public static bool IsAngleOver( PlanarFace pf, double angleByUserInput )
        {
            XYZ basis = XYZ.BasisZ;
            XYZ normal = pf.FaceNormal;

            double angle = basis.AngleTo( normal ) * 180 / Math.PI;
            if ( angleByUserInput <= angle )
                return true;

            return false;
        }


        public static Level FindLevelParameterByCategory( Document doc,
                                                          Element e,
                                                          List<Level> projectLevels )
        {
            ElementId levelId = null;
            BuiltInCategory hostCat = ( BuiltInCategory )e.Category.Id.IntegerValue;

            switch ( hostCat )
            {
                case BuiltInCategory.OST_Columns:
                {
                    Parameter p = e.get_Parameter( BuiltInParameter.SCHEDULE_BASE_LEVEL_PARAM );
                    if ( p != null )
                    {
                        levelId = p.AsElementId();
                    }
                    break;
                }
                case BuiltInCategory.OST_StructuralColumns:
                {
                    Parameter p = e.get_Parameter( BuiltInParameter.SCHEDULE_BASE_LEVEL_PARAM );
                    if ( p != null )
                    {
                        levelId = p.AsElementId();
                    }
                    break;
                }
                case BuiltInCategory.OST_StructuralFraming:
                {
                    Parameter p = e.get_Parameter( BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM );
                    if ( p != null )
                    {
                        levelId = p.AsElementId();
                    }
                    break;
                }
                case BuiltInCategory.OST_StructuralFoundation:
                {
                    Parameter p = e.get_Parameter( BuiltInParameter.FAMILY_LEVEL_PARAM );

                    if ( p == null )
                        p = e.get_Parameter( BuiltInParameter.LEVEL_PARAM );

                    if ( p != null )
                        levelId = p.AsElementId();

                    break;
                }
                case BuiltInCategory.OST_Floors:
                {
                    Parameter p = e.get_Parameter( BuiltInParameter.LEVEL_PARAM );
                    if ( p != null )
                    {
                        levelId = p.AsElementId();
                    }
                    break;
                }
                case BuiltInCategory.OST_Walls:
                {
                    Parameter p = e.get_Parameter( BuiltInParameter.WALL_BASE_CONSTRAINT );
                    if ( p != null )
                    {
                        levelId = p.AsElementId();
                    }
                    break;
                }
                case BuiltInCategory.OST_Stairs:
                {
                    Parameter p = e.get_Parameter( BuiltInParameter.STAIRS_BASE_LEVEL_PARAM );
                    if ( p != null )
                    {
                        levelId = p.AsElementId();
                    }
                    break;
                }
                case BuiltInCategory.OST_EdgeSlab:
                {
                    return null;
                }
            }

            if ( levelId != null )
                return doc.GetElement( levelId ) as Level;

            return null;
        }

        public static Level Get_LevelByCoord( Solid union, List<Level> projectLevels )
        {
            double number = union.ComputeCentroid().Z;

            double closest = projectLevels[ 0 ].Elevation;
            double difference = Math.Abs( number - closest );
            for ( int i = 1; i < projectLevels.Count; i++ )
            {
                double currentDifference = Math.Abs( number - projectLevels[ i ].Elevation );
                if ( currentDifference < difference )
                {
                    closest = projectLevels[ i ].Elevation;
                    difference = currentDifference;
                }
            }

            return projectLevels.Where( x => x.Elevation.Equals( closest ) ).FirstOrDefault();
        }

        public static List<Level> Get_ProjectLevels( Document doc )
        {
            List<Level> levels = new List<Level>();
            using ( var collector = new FilteredElementCollector( doc ) )
            {
                // プロジェクトLEVEL
                levels = collector.OfClass( typeof( Autodesk.Revit.DB.Level ) )?
                                  .OfCategory( BuiltInCategory.OST_Levels )?
                                  .Cast<Autodesk.Revit.DB.Level>()?
                                  .ToList();
            }
            return levels;
        }


        public static List<HostElement> GroupElementsByHost( Document doc,
                                                             List<ElementId> selectedIds )
        {
            List<HostElement> filtered = new List<HostElement>();
            List<HostElement> hostElements = new List<HostElement>();

            foreach ( ElementId selectedId in selectedIds )
            {
                Element selectedElem = doc.GetElement( selectedId );

                HostElement hostElement = Find_HostElement( new HostElement
                {
                    HostFinal = selectedElem,
                    SubElements = new List<Element>(),
                    Union = null,
                } );

                if ( hostElement.Union != null )
                {
                    hostElements.Add( hostElement );
                }
            }

            if ( hostElements.Count > 0 )
            {
                List<IGrouping<int, HostElement>> groups = hostElements.GroupBy( x => x.HostFinal.Id.IntegerValue )
                                                                       .ToList();
                foreach ( IGrouping<int, HostElement> group in groups )
                {
                    if ( !filtered.Exists( x => x.HostFinal.Id.IntegerValue.Equals( group.Key ) ) )
                    {
                        List<Element> filteredSubs = new List<Element>();

                        foreach ( HostElement hostElement in group )
                        {
                            foreach ( Element sub in hostElement.SubElements )
                            {
                                if ( !filteredSubs.Exists( x => x.Id.IntegerValue.Equals( sub.Id.IntegerValue ) ) )
                                {
                                    filteredSubs.Add( sub );
                                }
                            }

                            hostElement.SubElements = filteredSubs;
                            filtered.Add( hostElement );
                        }
                    }
                }

                filtered = filtered.DistinctBy( x => x.HostFinal.Id.IntegerValue ).ToList();
                foreach ( HostElement filteredElement in filtered )
                {
                    List<Solid> solids = new List<Solid>();
                    if ( filteredElement.Union != null )
                    {
                        solids.Add( filteredElement.Union );
                        foreach ( Element sub in filteredElement.SubElements )
                        {
                            var subSolids = GetElementSolids( sub );
                            if ( subSolids.Count < 1 )
                                continue;

                            foreach ( Solid subSolid in subSolids )
                            {
                                solids.Add( subSolid );
                            }
                        }

                        filteredElement.Solids = solids;
                        filteredElement.Union = Create_UnionSolid( solids );
                    }
                }
            }
            return filtered;
        }


        private static HostElement Find_HostElement( HostElement hostElement )
        {
            if ( hostElement.Union == null )
            {
                List<Solid> solids = GetElementSolids( hostElement.HostFinal );
                if ( solids.Count > 0 )
                {
                    hostElement.Union = Create_UnionSolid( solids );
                }
                else
                    return hostElement;
            }

            FamilyInstance fi = hostElement.HostFinal as FamilyInstance;
            if ( fi != null )
            {
                if ( fi.Host != null )
                {
                    List<Solid> hostSolids = GetElementSolids( fi.Host );
                    if ( hostSolids.Count > 0 )
                    {
                        hostSolids.Add( hostElement.Union );
                        hostElement.Union = Create_UnionSolid( hostSolids );
                        hostElement.HostFinal = fi.Host;
                        hostElement.SubElements.Add( fi );

                        return Find_HostElement( hostElement );
                    }
                    else
                        return hostElement;
                }
                else
                    return hostElement;
            }
            else
                return hostElement;
        }

        public static IList<CurveLoop> OptimizeCurveLoops( PlanarFace pf, double tolerance )
        {
            IList<CurveLoop> curveLoops = new List<CurveLoop>();

            foreach ( CurveLoop curveLoop in pf.GetEdgesAsCurveLoops() )
            {
                if ( curveLoop.IsOpen() )
                {
                    continue;
                }

                if ( !curveLoop.HasPlane() )
                {
                    continue;
                }

                List<XYZ> pts = new List<XYZ>();

                int noneCurveCount = curveLoop.Count( x => x.IsCyclic );
                if ( noneCurveCount > 0 )
                {
                    curveLoops.Add( curveLoop );
                    continue;
                }

                foreach ( Curve curve in curveLoop )
                {
                    if ( curve.Length > tolerance )
                    {
                        Line line = curve as Line;
                        if ( line != null )
                        {
                            pts.Add( curve.GetEndPoint( 0 ) );
                        }
                    }
                }

                if ( pts.Count > 2 )
                {
                    CurveLoop loop = CreateCurveLoop( pts );

                    if ( !loop.IsOpen() && loop.HasPlane() )
                    {
                        curveLoops.Add( loop );
                    }

                }

            }

            return curveLoops;
        }

        private static CurveLoop CreateCurveLoop( List<XYZ> pts )
        {
            var n = pts.Count;
            var curveLoop = new CurveLoop();
            for ( var i = 1; i < n; ++i )
            {
                curveLoop.Append( Line.CreateBound( pts[ i - 1 ], pts[ i ] ) );
            }
            curveLoop.Append( Line.CreateBound( pts[ n - 1 ], pts[ 0 ] ) );

            return curveLoop;
        }


        #region Util - Solid

        public static Solid Create_UnionSolid( List<Solid> solids )
        {
            Solid union = null;

            if ( solids.Count == 1 )
                return solids.FirstOrDefault();

            foreach ( Solid solid in solids )
            {
                if ( solid.Volume > 0 )
                {
                    try
                    {
                        if ( union == null )
                        {
                            union = solid;
                            continue;
                        }
                        else
                            union = BooleanOperationsUtils.ExecuteBooleanOperation( union,
                                                                                    solid,
                                                                                    BooleanOperationsType.Union );
                    }
                    catch ( Exception ) { continue; }
                }
            }
            return union;
        }

        public static bool IsSolidIntersect( Solid solid,
                                             Solid solidCompared )
        {
            if ( solid.Volume > 0.0 && solidCompared.Volume > 0.0 )
            {
                try
                {
                    Solid interSolid = BooleanOperationsUtils.ExecuteBooleanOperation( solid,
                                                                                       solidCompared,
                                                                                       BooleanOperationsType.Intersect );
                    Solid unionSolid = BooleanOperationsUtils.ExecuteBooleanOperation( solid,
                                                                                       solidCompared,
                                                                                       BooleanOperationsType.Union );

                    double sumArea = Math.Round( Math.Abs( solid.SurfaceArea + solidCompared.SurfaceArea ), 5 );
                    double sumFaces = Math.Abs( solid.Faces.Size + solidCompared.Faces.Size );
                    double unionArea = Math.Round( Math.Abs( unionSolid.SurfaceArea ), 5 );
                    double unionFaces = Math.Abs( unionSolid.Faces.Size );

                    if ( sumArea == unionArea && sumFaces == unionFaces && interSolid.Volume < 0.00001 )
                    {
                        // not touching, not intersecting
                        return false;
                    }
                    else if ( sumArea > unionArea && sumFaces > unionFaces && interSolid.Volume > 0.00001 )
                    {
                        // intersecting
                        return true;
                    }
                    else if ( sumArea > unionArea && sumFaces > unionFaces && interSolid.Volume < 0.00001 )
                    {
                        // touching
                        return true;
                    }
                    else
                        return false;
                }
                catch ( Exception )
                {
                    return false;
                }
            }
            return false;
        }

        public static List<Solid> GetElementSolids( Element element )
        {
            Options op = new Options
            {
                ComputeReferences = true,
                IncludeNonVisibleObjects = true
            };

            BuiltInCategory hostCat = ( BuiltInCategory )element.Category.Id.IntegerValue;
            if ( hostCat == BuiltInCategory.OST_Walls )
            {
                op.IncludeNonVisibleObjects = false;
            }

            List<Solid> solids = new List<Solid>();

            try
            {
                GeometryElement geoEle = element.get_Geometry( op );
                if ( geoEle != null )
                {
                    IEnumerator<GeometryObject> gIter = geoEle.GetEnumerator();
                    gIter.Reset();
                    while ( gIter.MoveNext() )
                    {
                        solids.AddRange( GetSolidsFromGeometryObj( gIter.Current ) );
                    }
                }
            }
            catch ( Exception ex )
            {
                TaskDialog.Show( "Error", ex.Message );
            }

            return solids;
        }

        public static List<Solid> GetElementSolids( Element element,
                                                    Options options )
        {
            List<Solid> solids = new List<Solid>();

            try
            {
                GeometryElement geoEle = element.get_Geometry( options );
                if ( geoEle != null )
                {
                    IEnumerator<GeometryObject> gIter = geoEle.GetEnumerator();
                    gIter.Reset();
                    while ( gIter.MoveNext() )
                    {
                        solids.AddRange( GetSolidsFromGeometryObj( gIter.Current ) );
                    }
                }
            }
            catch ( Exception ex )
            {
                TaskDialog.Show( "Error", ex.Message );
            }

            return solids;
        }

        private static List<Solid> GetSolidsFromGeometryObj( GeometryObject gObj )
        {
            List<Solid> solids = new List<Solid>();

            if ( gObj is Solid )
            {
                Solid solid = gObj as Solid;
                if ( solid.Faces.Size > 0 && Math.Abs( solid.Volume ) > 0 )
                {
                    solids.Add( gObj as Solid );
                }
            }
            else if ( gObj is GeometryInstance )
            {
                IEnumerator<GeometryObject> gIter2 = ( gObj as GeometryInstance ).GetInstanceGeometry().GetEnumerator();
                gIter2.Reset();
                while ( gIter2.MoveNext() )
                {
                    solids.AddRange( GetSolidsFromGeometryObj( gIter2.Current ) );
                }
            }

            else if ( gObj is GeometryElement )
            {
                IEnumerator<GeometryObject> gIter2 = ( gObj as GeometryElement ).GetEnumerator();
                gIter2.Reset();
                while ( gIter2.MoveNext() )
                {
                    solids.AddRange( GetSolidsFromGeometryObj( gIter2.Current ) );
                }
            }
            return solids;
        }

        #endregion

        #region Grid Utils

        public static List<Autodesk.Revit.DB.Grid> FindGridByDirection( XY gridXY,
                                                                        List<Autodesk.Revit.DB.Grid> grids,
                                                                        double range )
        {
            var result = new List<Autodesk.Revit.DB.Grid>();

            if ( grids.Count < 1 )
                return result;

            double rangeRadian = ( range * Math.PI ) / 180; // 10

            foreach ( var grid in grids )
            {
                var gridLine = grid.Curve as Line;

                if ( gridLine == null )
                    continue;


                var a = ( 180 * Math.PI ) / 180; // 180

                switch ( gridXY )
                {
                    case XY.X:
                    {
                        double angle = XYZ.BasisY.AngleTo( gridLine.Direction ); // 180

                        if ( angle <= rangeRadian )
                            result.Add( grid );
                        else if ( Math.Abs( a - angle ) <= rangeRadian )
                            result.Add( grid );
                        continue;
                    }
                    case XY.Y:
                    {
                        double angle = XYZ.BasisX.AngleTo( gridLine.Direction );

                        if ( angle <= rangeRadian )
                            result.Add( grid );
                        else if ( Math.Abs( a - angle ) <= rangeRadian )
                            result.Add( grid );
                        continue;
                    }
                }
            }
            return result;
        }

        public static List<Autodesk.Revit.DB.Grid> Get_Project_Grids( Document doc )
        {
            var projectGrids = new List<Autodesk.Revit.DB.Grid>();
            using ( var collector = new FilteredElementCollector( doc ) )
            {
                // プロジェクト通芯収集
                projectGrids = collector.OfClass( typeof( Autodesk.Revit.DB.Grid ) )?
                                        .OfCategory( BuiltInCategory.OST_Grids )?
                                        .Cast<Autodesk.Revit.DB.Grid>()?
                                        .ToList();
            }
            return projectGrids;
        }

        public static List<Autodesk.Revit.DB.Grid> Get_Grid_HostElementAround( Element hostElem,
                                                                               Autodesk.Revit.DB.View view,
                                                                               List<Autodesk.Revit.DB.Grid> projectGrids )
        {
            var grids = new List<Autodesk.Revit.DB.Grid>();

            BoundingBoxXYZ bbox = hostElem.get_BoundingBox( view );
            if ( bbox == null )
                return grids;

            // corners in BBox coords
            double add = UnitUtil.Millimeters_To_Feet( 500 ); // 周囲の通り芯を認識範囲を拡張
            XYZ pt0 = new XYZ( bbox.Min.X - add, bbox.Min.Y - add, bbox.Min.Z );
            XYZ pt1 = new XYZ( bbox.Max.X + add, bbox.Min.Y - add, bbox.Min.Z );
            XYZ pt2 = new XYZ( bbox.Max.X + add, bbox.Max.Y + add, bbox.Min.Z );
            XYZ pt3 = new XYZ( bbox.Min.X - add, bbox.Max.Y + add, bbox.Min.Z );
            //edges in BBox coords
            Line edge0 = Line.CreateBound( pt0, pt1 );
            Line edge1 = Line.CreateBound( pt1, pt2 );
            Line edge2 = Line.CreateBound( pt2, pt3 );
            Line edge3 = Line.CreateBound( pt3, pt0 );
            //create loop, still in BBox coords
            List<Curve> edges = new List<Curve>();
            edges.Add( edge0 );
            edges.Add( edge1 );
            edges.Add( edge2 );
            edges.Add( edge3 );
            double height = bbox.Max.Z - bbox.Min.Z;
            CurveLoop baseLoop = CurveLoop.Create( edges );
            List<CurveLoop> loopList = new List<CurveLoop>();
            loopList.Add( baseLoop );
            Solid preTransformBox = GeometryCreationUtilities.CreateExtrusionGeometry( loopList, XYZ.BasisZ, height );
            Solid transformBox = SolidUtils.CreateTransformed( preTransformBox, bbox.Transform );

            if ( transformBox.Volume > 0.0 )
            {
                XYZ centroid = transformBox.ComputeCentroid();


                foreach ( Autodesk.Revit.DB.Grid projectGrid in projectGrids )
                {
                    Curve gridCrv = projectGrid.Curve;
                    XYZ vec = new XYZ( 0, 0, centroid.Z - gridCrv.GetEndPoint( 0 ).Z );
                    Transform t = Transform.CreateTranslation( vec );
                    Curve transformedCrv = gridCrv.CreateTransformed( t );
                    SolidCurveIntersectionOptions op = new SolidCurveIntersectionOptions();
                    SolidCurveIntersection intersection = transformBox.IntersectWithCurve( transformedCrv, op );
                    if ( intersection.SegmentCount > 0 )
                    {
                        grids.Add( projectGrid );
                    }
                }
            }
            return grids;
        }

        #endregion

        public static void OverrideDisplayElement( Document doc,
                                                   Element element,
                                                   Autodesk.Revit.DB.Color color,
                                                   ElementId fillPatternId )
        {
            try
            {
                string revitVersion = doc.Application.VersionName;

                OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                if ( revitVersion.Contains( "2019" )
                  || revitVersion.Contains( "2020" )
                  || revitVersion.Contains( "2021" )
                  || revitVersion.Contains( "2022" ) )
                {
                    ogs.SetProjectionLineColor( new Autodesk.Revit.DB.Color( 0, 0, 0 ) );
                    ogs.SetSurfaceBackgroundPatternId( fillPatternId );
                    ogs.SetSurfaceBackgroundPatternId( fillPatternId );
                    ogs.SetSurfaceForegroundPatternColor( color );
                    ogs.SetSurfaceBackgroundPatternColor( color );
                }

                doc.ActiveView.SetElementOverrides( element.Id, ogs );
                return;
            }
            catch ( Exception ex )
            {
                return;
            }
        }

        public static ElementId GetSolidPatternId( Document doc )
        {
            try
            {
                FillPatternElement solidFill = null;
                ICollection<Element> patterns = new FilteredElementCollector( doc )
                  .OfClass( typeof( FillPatternElement ) )
                  .ToElements();

                foreach ( FillPatternElement pattern in patterns )
                {
                    string patternName = pattern.Name;
                    if ( patternName.Contains( "塗り潰し" )
                      || patternName.Contains( "Solid fill" ) )
                    {
                        solidFill = pattern;
                        break;
                    }
                    else
                        continue;
                }

                return solidFill?.Id;
            }
            catch ( Exception )
            {
                throw;
            }
        }

        public static System.Drawing.Color WpfBrushToDrawingColor( System.Windows.Media.SolidColorBrush brush )
        {
            return System.Drawing.Color.FromArgb(
                brush.Color.A,
                brush.Color.R,
                brush.Color.G,
                brush.Color.B );
        }

        public static int Get_CodeLineNumber( Exception ex )
        {
            var lineNumber = 0;
            const string lineSearch = ":line ";
            var index = ex.StackTrace.LastIndexOf( lineSearch );
            if ( index != -1 )
            {
                var lineNumberText = ex.StackTrace.Substring( index + lineSearch.Length );
                if ( int.TryParse( lineNumberText, out lineNumber ) )
                {
                }
            }
            return lineNumber;
        }


        public static bool IsInvalid( this ElementId id )
        {
            return ElementId.InvalidElementId == id;
        }


        public static bool IsValid( this ElementId id )
        {
            return !IsInvalid( id );
        }

    }

    public enum XY
    {
        X, Y,
    }
}
