using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;


namespace BIM.RevitCommand.Formwork.Util
{
    internal enum FormworkParameter
    {
        None,
        Level,
        GridX,
        GridY,
        Category,
        Family,
        FamilyType,
        Area,
        HostId
    }


    internal class FormworkParamUtil
    {

        private static List<string> parameterNames = new List<string>()
        {
            "#Formwork_Level",
            "#Formwork_Grid_X",
            "#Formwork_Grid_Y",
            "#Formwork_Category",
            "#Formwork_Family",
            "#Formwork_TypeName",
            "#Formwork_Area",
            "#Formwork_HostId",
        };

        public static FormworkParameter GetEmum( string parameterName )
        {
            if ( parameterNames[ 0 ].Equals( parameterName ) )
                return FormworkParameter.Level;
            else if ( parameterNames[ 1 ].Equals( parameterName ) )
                return FormworkParameter.GridX;
            else if ( parameterNames[ 2 ].Equals( parameterName ) )
                return FormworkParameter.GridY;
            else if ( parameterNames[ 3 ].Equals( parameterName ) )
                return FormworkParameter.Category;
            else if ( parameterNames[ 4 ].Equals( parameterName ) )
                return FormworkParameter.Family;
            else if ( parameterNames[ 5 ].Equals( parameterName ) )
                return FormworkParameter.FamilyType;
            else if ( parameterNames[ 6 ].Equals( parameterName ) )
                return FormworkParameter.Area;
            else if ( parameterNames[ 7 ].Equals( parameterName ) )
                return FormworkParameter.HostId;
            else
                return FormworkParameter.None;
        }

        public static string GetName( FormworkParameter parameter )
        {
            switch ( parameter )
            {
                case FormworkParameter.Level:
                    return parameterNames[ 0 ];
                case FormworkParameter.GridX:
                    return parameterNames[ 1 ];
                case FormworkParameter.GridY:
                    return parameterNames[ 2 ];
                case FormworkParameter.Category:
                    return parameterNames[ 3 ];
                case FormworkParameter.Family:
                    return parameterNames[ 4 ];
                case FormworkParameter.FamilyType:
                    return parameterNames[ 5 ];
                case FormworkParameter.Area:
                    return parameterNames[ 6 ];
                case FormworkParameter.HostId:
                    return parameterNames[ 7 ];
            }
            return null;
        }


        public static void CreateSharedParameter( Document doc )
        {

            // 공유파라메터 정의
            ExternalDefinition paramTypeName = GetOrCreateDef( GetName( FormworkParameter.FamilyType ), SpecTypeId.String.Text, doc.Application );
            ExternalDefinition paramHostId = GetOrCreateDef( GetName( FormworkParameter.HostId ), SpecTypeId.Int.Integer, doc.Application );
            ExternalDefinition paramGridXName = GetOrCreateDef( GetName( FormworkParameter.GridX ), SpecTypeId.String.Text, doc.Application );
            ExternalDefinition paramGridYName = GetOrCreateDef( GetName( FormworkParameter.GridY ), SpecTypeId.String.Text, doc.Application );
            ExternalDefinition paramLevelName = GetOrCreateDef( GetName( FormworkParameter.Level ), SpecTypeId.String.Text, doc.Application );
            ExternalDefinition paramArea = GetOrCreateDef( GetName( FormworkParameter.Area ), SpecTypeId.Area, doc.Application );
            ExternalDefinition paramCategory = GetOrCreateDef( GetName( FormworkParameter.Category ), SpecTypeId.String.Text, doc.Application );
            ExternalDefinition paramFamily = GetOrCreateDef( GetName( FormworkParameter.Family ), SpecTypeId.String.Text, doc.Application );


            // 대상 카테고리 정의
            //Category cat_frame = doc.Settings.Categories.get_Item( BuiltInCategory.OST_StructuralFraming );
            //Category cat_column_s = doc.Settings.Categories.get_Item( BuiltInCategory.OST_StructuralColumns );
            //Category cat_column = doc.Settings.Categories.get_Item( BuiltInCategory.OST_Columns );
            //Category cat_wall = doc.Settings.Categories.get_Item( BuiltInCategory.OST_Walls );
            //Category cat_floor = doc.Settings.Categories.get_Item( BuiltInCategory.OST_Floors );
            //Category cat_edgeSlab = doc.Settings.Categories.get_Item( BuiltInCategory.OST_EdgeSlab );
            //Category cat_foundation = doc.Settings.Categories.get_Item( BuiltInCategory.OST_StructuralFoundation );
            Category cat_genericModel = doc.Settings.Categories.get_Item( BuiltInCategory.OST_GenericModel );


            CategorySet catSet = doc.Application.Create.NewCategorySet();
            //catSet.Insert( cat_frame );
            //catSet.Insert( cat_column_s );
            //catSet.Insert( cat_column );
            //catSet.Insert( cat_wall );
            //catSet.Insert( cat_floor );
            //catSet.Insert( cat_edgeSlab );
            //catSet.Insert( cat_foundation );
            catSet.Insert( cat_genericModel );


            DefinitionFile defFile = GetSharedParameterFile( doc.Application );
            foreach ( DefinitionGroup defGroup in defFile.Groups )
            {
                if ( defGroup.Name.Equals( "Formwork-Link" ) )
                {
                    foreach ( ExternalDefinition def in defGroup.Definitions )
                    {
                        InstanceBinding newIb = doc.Application.Create.NewInstanceBinding( catSet );
                        doc.ParameterBindings.Insert( def, newIb, BuiltInParameterGroup.PG_IDENTITY_DATA );
                    }
                }
            }
        }


        public static ElementId GetOrCreateDef( string parameterName,
                                                ForgeTypeId specTypeId,
                                                Document revitDoc )
        {
            ExternalDefinition ed = GetOrCreateDef( parameterName, specTypeId, revitDoc.Application );
            return RebarShapeParameters.GetOrCreateElementIdForExternalDefinition( revitDoc, ed );
        }


        public static ExternalDefinition GetOrCreateDef( string parameterName, ForgeTypeId specTypeId, Application revitApp )
        {
            return GetOrCreateDef( parameterName, "Formwork-Link", specTypeId, revitApp );
        }


        public static ExternalDefinition GetOrCreateDef( string name, string groupName, ForgeTypeId specTypeId, Application revitApp )
        {
            DefinitionFile parameterFile = GetSharedParameterFile( revitApp );

            DefinitionGroup group = parameterFile.Groups.get_Item( groupName );
            if ( group == null )
                group = parameterFile.Groups.Create( groupName );

            ExternalDefinition Bdef = group.Definitions.get_Item( name ) as ExternalDefinition;
            if ( Bdef == null )
            {
                var ExternalDefinitionCreationOptions = new ExternalDefinitionCreationOptions( name, specTypeId );
                Bdef = group.Definitions.Create( ExternalDefinitionCreationOptions ) as ExternalDefinition;
            }

            return Bdef;
        }


        public static DefinitionFile GetSharedParameterFile( Application revitApp )
        {
            DefinitionFile file = null;

            int count = 0;

            while ( null == file && count < 100 )
            {
                file = revitApp.OpenSharedParameterFile();
                if ( file == null )
                {
                    string path = Util.FileUtil.GetDirectory( Util.FileUtil.FolderName2022.Addins )
                           + "SharedParamFormwork.txt";

                    if ( !File.Exists( path ) )
                    {
                        System.Text.StringBuilder contents = new System.Text.StringBuilder();
                        contents.AppendLine( "# This is a Revit shared parameter file." );
                        contents.AppendLine( "# Do not edit manually." );
                        contents.AppendLine( "*META	VERSION	MINVERSION" );
                        contents.AppendLine( "META	2	1" );
                        contents.AppendLine( "*GROUP	ID	NAME" );
                        contents.AppendLine( "*PARAM	GUID	NAME	DATATYPE	DATACATEGORY	GROUP	VISIBLE" );

                        File.WriteAllText( path, contents.ToString() );
                    }

                    revitApp.SharedParametersFilename = path;
                }

                ++count;
            }

            return file;
        }

    }
}
