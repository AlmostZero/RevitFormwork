using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using System.Diagnostics;

namespace BIM.RevitCommand.Formwork.Util
{
    public static class XmlUtil
    {
        public static bool IsExistXmlFile( String xmlPath,
                                           String xmlName )
        {
            string path = xmlPath;

            if ( !path.EndsWith( "\\" ) )
                path += "\\";

            if ( File.Exists( path + xmlName ) )
                return true;
            else
                return false;
        }

        public static void SaveEmbededXML( FileUtil.FolderName2022 saveFolder,
                                           string saveXml )
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml( saveXml );
            doc.Save( FileUtil.GetDirectory( saveFolder ) + FileUtil.DEFAULT_XML_NAME );
        }

        public static bool WriteXML<T>( T serializeObj,
                                        String xmlPath,
                                        String xmlName )
        {
            try
            {
                if ( serializeObj == null )
                    return false;

                if ( !Directory.Exists( xmlPath ) )
                    Directory.CreateDirectory( xmlPath );

                if ( Directory.Exists( xmlPath ) )
                {
                    string path = xmlPath;
                    if ( !path.EndsWith( "\\" ) )
                    {
                        path += "\\";
                    }

                    XmlSerializer writer = new XmlSerializer( serializeObj.GetType() );

                    XmlWriterSettings setting = new XmlWriterSettings()
                    {
                        Indent = true,
                        NewLineOnAttributes = true,
                        Encoding = Encoding.UTF8,
                        WriteEndDocumentOnClose = true,
                        CloseOutput = true
                    };

                    FileStream fileStream = File.Create( path + xmlName );

                    using ( XmlWriter xmlWriter = XmlWriter.Create( fileStream, setting ) )
                    {
                        writer.Serialize( xmlWriter, serializeObj );
                    }

                    //FileStream fileStream = File.Create( path + xmlName );
                    //writer.Serialize( fileStream, serializeObj );
                    //fileStream.Close();

                    return true;
                }
                else
                    return false;
            }
            catch ( Exception ex )
            {
                ShowMsg_Error( "WriteXML()", ex.Message );
                return false;
            }
        }


        public static Object LoadXML<T>( String xmlPath,
                                         String xmlName )
        {
            string path = xmlPath;
            if ( !path.EndsWith( "\\" ) )
            {
                path += "\\";
            }

            if ( File.Exists( path + xmlName ) )
            {
                T xml;
                // Create an instance of the XmlSerializer.
                var serializer = new XmlSerializer( typeof( T ) );

                // Declare an object variable of the type to be deserialized.      
                using ( Stream reader = new FileStream( path + xmlName, FileMode.Open ) )
                {
                    // Call the Deserialize method to restore the object's state.
                    try
                    {
                        xml = ( T )serializer.Deserialize( reader );
                    }
                    catch ( Exception )
                    {
                        // Deserializeが失敗したファイルを無視する。
                        return null;
                    }
                }
                return xml;
            }
            else
                return null;
        }

        public static void ShowMsg_Error( string caption,
                                          string msg )
        {
            Debug.WriteLine( msg );
            System.Windows.Forms.MessageBox.Show( msg,
                                                  caption,
                                                  System.Windows.Forms.MessageBoxButtons.OK,
                                                  System.Windows.Forms.MessageBoxIcon.Error );
        }
    }
}
