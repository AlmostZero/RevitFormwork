using System.Linq;
using Autodesk.Revit.DB;


namespace BIM.RevitCommand.Formwork.Util
{
    internal class ScheduleUtil
    {

        public static ViewSchedule Create_Formwork_Schedule( Autodesk.Revit.DB.Document doc,
                                                             string scheduleTitle,
                                                             string dsTypeName = "JBK_FACE_DS" )
        {
            ElementId dsTypeId = GetFormworkTypeId( doc, dsTypeName );

            if ( dsTypeId == null )
                return null;

            if ( IsExistScheduleTitle( doc, scheduleTitle ) )
                return null;

            ViewSchedule schedule = ViewSchedule.CreateSchedule( doc,
                                                 new ElementId( BuiltInCategory.OST_GenericModel ),
                                                 ElementId.InvalidElementId );
            schedule.Name = scheduleTitle;

            ScheduleFieldId[] fields = new ScheduleFieldId[ 9 ];
            ElementId typeParamId = new ElementId( BuiltInParameter.ELEM_TYPE_PARAM );

            foreach ( SchedulableField schedulableField in schedule.Definition.GetSchedulableFields() )
            {
                ElementId parameterId = schedulableField.ParameterId;

                // 1. Collection of Shared Parameters
                SharedParameterElement sharedParam = GetSharedParameter( doc, parameterId );
                if ( sharedParam != null )
                {
                    switch ( FormworkParamUtil.GetEmum( sharedParam.Name ) )
                    {
                        case FormworkParameter.Level:
                        {
                            ScheduleField field_LevelName = schedule.Definition.AddField( schedulableField );
                            field_LevelName.ColumnHeading = "レベル";
                            //field_LevelName.Definition.
                            fields[ 0 ] = field_LevelName.FieldId;
                            continue;
                        }
                        case FormworkParameter.GridX:
                        {
                            ScheduleField field_GridX = schedule.Definition.AddField( schedulableField );
                            field_GridX.ColumnHeading = "X方向";
                            fields[ 1 ] = field_GridX.FieldId; ;
                            continue;
                        }
                        case FormworkParameter.GridY:
                        {
                            ScheduleField field_GridY = schedule.Definition.AddField( schedulableField );
                            field_GridY.ColumnHeading = "Y方向";
                            fields[ 2 ] = field_GridY.FieldId;
                            continue;
                        }
                        case FormworkParameter.Category:
                        {
                            ScheduleField field_CatName = schedule.Definition.AddField( schedulableField );
                            field_CatName.ColumnHeading = "カテゴリ名";
                            fields[ 3 ] = field_CatName.FieldId;
                            continue;
                        }
                        case FormworkParameter.Family:
                        {
                            ScheduleField field_FamilyName = schedule.Definition.AddField( schedulableField );
                            field_FamilyName.ColumnHeading = "ファミリ名";
                            fields[ 4 ] = field_FamilyName.FieldId;
                            continue;
                        }
                        case FormworkParameter.FamilyType:
                        {
                            ScheduleField field_FamiyTypeName = schedule.Definition.AddField( schedulableField );
                            field_FamiyTypeName.ColumnHeading = "タイプ名";
                            fields[ 5 ] = field_FamiyTypeName.FieldId;
                            continue;
                        }
                        case FormworkParameter.Area:
                        {
                            ScheduleField field_Area = schedule.Definition.AddField( schedulableField );
                            field_Area.ColumnHeading = "型枠面積(㎡)";

                            FormatOptions formatOptions = new FormatOptions();
                            formatOptions.UseDefault = false;
                            formatOptions.RoundingMethod = RoundingMethod.Nearest;
                            formatOptions.Accuracy = 0.001;
                            formatOptions.SuppressTrailingZeros = true;
                            formatOptions.SetUnitTypeId( UnitTypeId.SquareMeters );
                            formatOptions.SetSymbolTypeId( SymbolTypeId.MSup2 );
                            formatOptions.IsValidForSpec( SpecTypeId.Area );
                            field_Area.SetFormatOptions( formatOptions );

                            field_Area.DisplayType = ScheduleFieldDisplayType.Totals;
                            field_Area.HorizontalAlignment = ScheduleHorizontalAlignment.Right;
                            fields[ 6 ] = field_Area.FieldId;
                            break;
                        }
                        case FormworkParameter.HostId:
                        {
                            ScheduleField field_HostId = schedule.Definition.AddField( schedulableField );
                            field_HostId.ColumnHeading = "ホストID";
                            field_HostId.HorizontalAlignment = ScheduleHorizontalAlignment.Center;
                            fields[ 7 ] = field_HostId.FieldId;
                            continue;
                        }
                        case FormworkParameter.None:
                            continue;
                    }
                }

                // 2. Type Parameter
                if ( parameterId == typeParamId )
                {
                    ScheduleField field_DsTypeName = schedule.Definition.AddField( schedulableField );
                    field_DsTypeName.ColumnHeading = "DSTypeName";

                    ScheduleFilter filter = new ScheduleFilter( field_DsTypeName.FieldId, ScheduleFilterType.Equal, dsTypeId );

                    field_DsTypeName.IsHidden = true;
                    field_DsTypeName.Definition.AddFilter( filter );

                    fields[ 8 ] = field_DsTypeName.FieldId;
                    continue;
                }
            }

            if ( schedule != null )
            {
                schedule.Definition.SetFieldOrder( fields.ToList() );
                ScheduleSortGroupField sortGroup_Level = new ScheduleSortGroupField( fields[ 0 ] )
                {
                    ShowFooter = true,
                    ShowHeader = true,
                    ShowBlankLine = false,
                };

                schedule.Definition.AddSortGroupField( sortGroup_Level );
                schedule.Definition.ShowGrandTotal = true;
            }

            return schedule;
        }


        private static bool IsExistScheduleTitle( Autodesk.Revit.DB.Document doc,
                                                  string scheduleTitle )
        {
            using ( var collection = new FilteredElementCollector( doc ) )
            {
                var viewSchedule = collection.OfClass( typeof( ViewSchedule ) )
                                             .OfCategory( BuiltInCategory.OST_Schedules )
                                             .Where( x => x.Name == scheduleTitle )
                                             .FirstOrDefault();
                if ( viewSchedule != null )
                    return true;
            }
            return false;
        }


        private static SharedParameterElement GetSharedParameter( Autodesk.Revit.DB.Document doc,
                                                                  ElementId parameterId )
        {
            var sharedParameterElement = doc.GetElement( parameterId ) as SharedParameterElement;
            return sharedParameterElement;
        }


        private static ElementId GetFormworkTypeId( Autodesk.Revit.DB.Document doc,
                                                    string dsTypeName )
        {
            ElementId typeId = null;
            using ( var collection = new FilteredElementCollector( doc ) )
            {
                Element ds = collection.OfClass( typeof( DirectShape ) )
                                       .OfCategory( BuiltInCategory.OST_GenericModel )
                                       .Where( x => x.Name == dsTypeName )
                                       .FirstOrDefault();
                if ( ds != null )
                    typeId = ds.GetTypeId();
            }
            return typeId;
        }


    }
}
