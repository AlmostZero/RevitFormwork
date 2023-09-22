using System;
using System.Windows.Interop;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using BIM.RevitCommand.Formwork.MVVM.View;
using BIM.RevitCommand.Formwork.Util;
using BIM.RevitCommand.Formwork.MVVM.ViewModel;
using BIM.RevitCommand.Formwork.Handler;

namespace BIM.RevitCommand.Formwork
{
    [Transaction( TransactionMode.Manual )]
    public class CmdFormworkUI : IExternalCommand
    {
        private UIApplication uiapp;
        private UIDocument uidoc;
        private Document doc;
        private MainView mainView;

        private static RequestHandler handler = new RequestHandler();
        private static ExternalEvent exEvent = ExternalEvent.Create( handler );

        public Result Execute( ExternalCommandData commandData,
                               ref string message,
                               ElementSet elements )
        {
            uiapp = commandData.Application;
            uidoc = uiapp.ActiveUIDocument;
            doc = uidoc.Document;

            string dir = Util.FileUtil.GetDirectory( Util.FileUtil.FolderName2022.Addins );
            string xmlName = Util.FileUtil.DEFAULT_XML_NAME;

            mainView = new MainView( exEvent, handler, uiapp )
            {
                ViewModel = XmlUtil.LoadXML<FormworkViewModel>( dir, xmlName ) as FormworkViewModel
            };

            try
            {
                //bool existXml = File.Exists( FileManager.FileUtil.GetDirectory( FileManager.FileUtil.FolderName2022.C );

                // Revit window를 Owner window로 설정
                IWin32Window revitWindow = new WPFWindow( Autodesk.Windows.ComponentManager.ApplicationWindow );
                WindowInteropHelper helper = new WindowInteropHelper( mainView );
                helper.Owner = revitWindow.Handle;

                mainView.Show();

                return Result.Succeeded;
            }
            catch ( Exception ex )
            {
                TaskDialog.Show( "CmdFormworkSchedule.Execute()", ex.Message );
                return Result.Failed;
            }

        }

        public static string GetPath()
        {
            return typeof( CmdFormworkUI ).Namespace + "." + nameof( CmdFormworkUI );
        }
    }

    class WPFWindow : System.Windows.Interop.IWin32Window
    {
        IntPtr hwnd;

        public WPFWindow( IntPtr h )
        {
            System.Diagnostics.Debug.Assert(
              IntPtr.Zero != h,
              "expected non-null window handle" );

            this.hwnd = h;
        }

        public IntPtr Handle
        {
            get { return this.hwnd; }
        }
    }
}
