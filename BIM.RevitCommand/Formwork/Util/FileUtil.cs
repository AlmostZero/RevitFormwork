using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIM.RevitCommand.Formwork.Util
{
    public static class FileUtil
    {

        public const String DEFAULT_XML_NAME = "formwork.xml";

        public enum FolderName2022
        {
            Addins,
            ProgramData,
        }

        private static string[] appFolders2022 =
        {
           @"C:\ProgramData\Autodesk\Revit\Addins\",
           @"C:\ProgramData\",
        };

        public static string GetDirectory( FolderName2022 name )
        {
            return appFolders2022[ ( int )name ];
        }

        public static void CreateDirectory( FolderName2022 name )
        {
            var total = appFolders2022.Length;
            for ( int i = 0; i < total; i++ )
            {
                var dirName = GetDirectory( name );
                if ( !Directory.Exists( dirName ) )
                {
                    Directory.CreateDirectory( dirName );
                }
            }
        }

        public static void CreateDirectory()
        {
            var total = appFolders2022.Length;
            for ( int i = 0; i < total; i++ )
            {
                if ( !Directory.Exists( appFolders2022[ i ] ) )
                {
                    Directory.CreateDirectory( appFolders2022[ i ] );
                }
            }
        }
    }
}
