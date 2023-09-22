using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIM.RevitCommand.Formwork.MVVM.ViewModel;
using BIM.RevitCommand.Formwork.MVVM.Model;
using BIM.RevitCommand.Formwork.Util;
using BIM.RevitCommand.Formwork.Handler;
using static System.Net.Mime.MediaTypeNames;
using BIM.RevitCommand.Properties;


namespace BIM.RevitCommand.Formwork.MVVM.View
{
    public partial class MainView : Window
    {
        private RequestHandler handler;
        private ExternalEvent exEvent;

        private UIApplication uiapp;
        public UIApplication UIApp
        {
            get { return uiapp; }
            set { uiapp = value; }
        }

        private UIDocument uidoc;
        private Document doc;

        private long milliseconds = 1;

        private Stopwatch stopwatch { get; set; }

        public bool IsClosed { get; private set; }


        public FormworkViewModel ViewModel { get; set; }


        public MainView( ExternalEvent exEvent,
                         RequestHandler handler,
                         UIApplication uiapp )
        {
            InitializeComponent();
            InitializaStopwatch();

            this.exEvent = exEvent;
            this.handler = handler;
            this.uiapp = uiapp;
            this.uidoc = this.uiapp.ActiveUIDocument;
            this.doc = this.uidoc.Document;
            this.handler.MainView = this;

            #region Icon
            Bitmap bmp = new Bitmap( Images.code_16 );
            IntPtr Hicon = bmp.GetHicon();
            this.Icon = Imaging.CreateBitmapSourceFromHIcon( Hicon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions() );
            #endregion

            this.PreviewKeyUp += new KeyEventHandler( HandleEsc );
            this.Closed += ( s, e ) => { IsClosed = true; };

        }


        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            if ( ViewModel != null )
                this.DataContext = ViewModel;
            else
                this.DataContext = Get_DefaultViewModel();
        }


        private FormworkViewModel Get_DefaultViewModel()
        {
            BrushConverter bc = new BrushConverter();

            this.ViewModel = new FormworkViewModel
            {
                SavedTime = DateTime.Now,
                XmlFileName = Util.FileUtil.DEFAULT_XML_NAME,
                PanelThk = 12.5,
                SkipLength = 0.0,
                SkipArea = 0.0,
                Angle = 30,
                IsCreateGroup = true,
                IsCreateRevitSchedules = true,
                IsExportExcel = false,
                FormworkModels = new ObservableCollection<FormworkModel>(),
                CategoryModels = new ObservableCollection<CategoryModel>()
                {
                    new CategoryModel
                    {
                        FormworkColor = new FormworkColor
                        {
                             Brush = ( SolidColorBrush )bc.ConvertFrom( "#b8e994" ),
                        },
                        IsSelectedCategory = true,
                        BuiltInCategory = BuiltInCategory.OST_Columns,
                        CategoryName = "柱",
                        Elements = new ObservableCollection<Element>(),
                    },
                    new CategoryModel
                    {
                        FormworkColor = new FormworkColor
                        {
                             Brush = ( SolidColorBrush )bc.ConvertFrom( "#78e08f" ),
                        },
                        IsSelectedCategory = true,
                        BuiltInCategory = BuiltInCategory.OST_StructuralColumns,
                        CategoryName = "構造柱",
                        Elements = new ObservableCollection<Element>(),
                    },
                    new CategoryModel
                    {
                        FormworkColor = new FormworkColor
                        {
                              Brush = ( SolidColorBrush )bc.ConvertFrom( "#38ada9" ),
                        },
                        IsSelectedCategory = true,
                        BuiltInCategory = BuiltInCategory.OST_StructuralFraming,
                        CategoryName = "構造フレーム",
                        Elements = new ObservableCollection<Element>(),
                    },
                    new CategoryModel
                    {
                        FormworkColor = new FormworkColor
                        {
                              Brush = ( SolidColorBrush )bc.ConvertFrom( "#079992" ),
                        },
                        IsSelectedCategory = true,
                        BuiltInCategory = BuiltInCategory.OST_StructuralFoundation,
                        CategoryName = "構造基礎",
                        Elements = new ObservableCollection<Element>(),
                    },
                    new CategoryModel
                    {
                        FormworkColor = new FormworkColor
                        {
                            Brush = ( SolidColorBrush )bc.ConvertFrom( "#82ccdd" ),
                        },
                        IsSelectedCategory = true,
                        BuiltInCategory = BuiltInCategory.OST_Floors,
                        CategoryName = "床",
                        Elements = new ObservableCollection<Element>(),
                    },
                    new CategoryModel
                    {
                        FormworkColor = new FormworkColor
                        {
                            Brush = ( SolidColorBrush )bc.ConvertFrom( "#3c6382" ),
                        },
                        IsSelectedCategory = true,
                        BuiltInCategory = BuiltInCategory.OST_Walls,
                        CategoryName = "壁",
                        Elements = new ObservableCollection<Element>(),
                    },
                    new CategoryModel
                    {
                        FormworkColor = new FormworkColor
                        {
                           Brush = ( SolidColorBrush )bc.ConvertFrom( "#82ccdd" ),
                        },
                        IsSelectedCategory = true,
                        BuiltInCategory = BuiltInCategory.OST_Stairs,
                        CategoryName = "階段",
                        Elements = new ObservableCollection<Element>(),
                    },
                    new CategoryModel
                    {
                        FormworkColor = new FormworkColor
                        {
                             Brush = ( SolidColorBrush )bc.ConvertFrom( "#82ccdd" ),
                        },
                        IsSelectedCategory = true,
                        BuiltInCategory = BuiltInCategory.OST_EdgeSlab,
                        CategoryName = "スラブエッジ",
                        Elements = new ObservableCollection<Element>(),
                    },
                },
            };
            return this.ViewModel;
        }

        private void btn_createFormwork_Click( object sender, RoutedEventArgs e )
        {
            MakeRequest( RequestId.CreateFormwork );
        }

        #region ProgressBar Implementation

        private void InitializaStopwatch()
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        public bool Run( ProgressBar pbar, string title, int count, Action<int> action )
        {
            this.Title = title;
            return Run( pbar, count, action );
        }


        public bool Run( ProgressBar pbar, int count, Action<int> action )
        {
            if ( IsClosed ) return IsClosed;

            Show();

            this.progressBar1.Value = 0;
            this.progressBar1.Maximum = count;
            for ( int i = 0; i < count; i++ )
            {
                action?.Invoke( i );
                if ( Update( pbar ) )
                    break;
            }
            return IsClosed;
        }


        public bool Run<T>( ProgressBar pbar, string title, IEnumerable<T> collection, Action<T> action )
        {
            this.Title = title;
            return Run( pbar, collection, action );
        }


        public bool Run<T>( ProgressBar pbar, IEnumerable<T> collection, Action<T> action )
        {
            if ( IsClosed )
                return IsClosed;

            //Show();

            pbar.Value = 0;
            pbar.Maximum = collection.Count();

            foreach ( var item in collection )
            {
                action?.Invoke( item );
                if ( Update( pbar ) )
                {
                    break;
                }
            }
            return IsClosed;
        }

        private bool Update( ProgressBar pbar, double value = 1.0 )
        {
            pbar.Value += value;
            if ( stopwatch.ElapsedMilliseconds > milliseconds )
            {
                DoEvents();
                stopwatch.Restart();
            }
            return IsClosed;
        }

        private void DoEvents()
        {
            System.Windows.Forms.Application.DoEvents();
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
        }

        public void Dispose()
        {
            if ( !IsClosed )
                Close();
        }
        #endregion

        private void HandleEsc( object sender, KeyEventArgs e )
        {
            if ( e.Key == Key.Escape )
            {
                this.Close();
            }
        }

        private void btn_close_Click( object sender, RoutedEventArgs e )
        {
            this.Close();
        }


        private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            try // Overwrite Xml File
            {
                var viewModel = this.DataContext as FormworkViewModel;
                if ( viewModel != null )
                {
                    viewModel.XmlFileName = Util.FileUtil.DEFAULT_XML_NAME;
                    string dir = Util.FileUtil.GetDirectory( Util.FileUtil.FolderName2022.Addins );
                    XmlUtil.WriteXML( viewModel, dir, viewModel.XmlFileName );
                }
            }
            catch ( Exception ex )
            {
                return;
            }
        }

        /// <summary>
        /// Transaction을 수행하기 위한 메서드를 실행한다.
        /// </summary>
        /// <param name="request"></param>
        private void MakeRequest( RequestId request )
        {
            handler.Request.Make( request );
            exEvent.Raise();
        }

        private void txtBox_panelThk_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            Regex regex = new Regex( "^[.][0-9]+$|^[0-9]*[.]{0,1}[0-9]*$" );
            e.Handled = !regex.IsMatch( ( sender as System.Windows.Controls.TextBox ).Text
                .Insert( ( sender as System.Windows.Controls.TextBox ).SelectionStart, e.Text ) );
        }

        private void btn_add_material_Click( object sender, RoutedEventArgs e )
        {

        }

        private void btn_remove_material_Click( object sender, RoutedEventArgs e )
        {

        }

        private void btn_openColorPalette_Click( object sender, RoutedEventArgs e )
        {
            try
            {
                CategoryModel currentModel = listBox_category.SelectedItem as CategoryModel;

                if ( currentModel == null )
                    return;

                var dialog = new System.Windows.Forms.ColorDialog()
                {
                    Color = FormworkUtil.WpfBrushToDrawingColor( currentModel.FormworkColor.Brush ),
                    AllowFullOpen = true,
                    AnyColor = true,
                    FullOpen = true
                };

                if ( dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK )
                {
                    var r = dialog.Color.R;
                    var g = dialog.Color.G;
                    var b = dialog.Color.B;

                    currentModel.FormworkColor = new FormworkColor()
                    {
                        Brush = new SolidColorBrush( System.Windows.Media.Color.FromRgb( r, g, b ) ),
                        RevitColor = new Autodesk.Revit.DB.Color( r, g, b )
                    };
                }
            }
            catch { return; }
        }

        private void btn_openColorPalette_MouseRightButtonUp( object sender, MouseButtonEventArgs e )
        {
            try
            {
                var currentModel = listBox_category.SelectedItem as CategoryModel;

                if ( currentModel == null )
                    return;

                var bc = new BrushConverter();

                var r = currentModel.Color.R;
                var g = currentModel.Color.G;
                var b = currentModel.Color.B;

                switch ( currentModel.BuiltInCategory )
                {
                    case BuiltInCategory.OST_Columns:
                    {
                        currentModel.FormworkColor = new FormworkColor()
                        {
                            Brush = ( SolidColorBrush )bc.ConvertFrom( "#b8e994" ),
                            RevitColor = new Autodesk.Revit.DB.Color( r, g, b )
                        };
                        break;
                    }
                    case BuiltInCategory.OST_StructuralColumns:
                    {
                        currentModel.FormworkColor = new FormworkColor()
                        {
                            Brush = ( SolidColorBrush )bc.ConvertFrom( "#78e08f" ),
                            RevitColor = new Autodesk.Revit.DB.Color( r, g, b )
                        };
                        break;
                    }
                    case BuiltInCategory.OST_StructuralFraming:
                    {
                        currentModel.FormworkColor = new FormworkColor()
                        {
                            Brush = ( SolidColorBrush )bc.ConvertFrom( "#38ada9" ),
                            RevitColor = new Autodesk.Revit.DB.Color( r, g, b )
                        };
                        break;
                    }
                    case BuiltInCategory.OST_StructuralFoundation:
                    {
                        currentModel.FormworkColor = new FormworkColor()
                        {
                            Brush = ( SolidColorBrush )bc.ConvertFrom( "#079992" ),
                            RevitColor = new Autodesk.Revit.DB.Color( r, g, b )
                        };
                        break;
                    }
                    case BuiltInCategory.OST_Floors:
                    {
                        currentModel.FormworkColor = new FormworkColor()
                        {
                            Brush = ( SolidColorBrush )bc.ConvertFrom( "#82ccdd" ),
                            RevitColor = new Autodesk.Revit.DB.Color( r, g, b )
                        };
                        break;
                    }
                    case BuiltInCategory.OST_Walls:
                    {
                        currentModel.FormworkColor = new FormworkColor()
                        {
                            Brush = ( SolidColorBrush )bc.ConvertFrom( "#3c6382" ),
                            RevitColor = new Autodesk.Revit.DB.Color( r, g, b )
                        };
                        break;
                    }
                    case BuiltInCategory.OST_Stairs:
                    {
                        currentModel.FormworkColor = new FormworkColor()
                        {
                            Brush = ( SolidColorBrush )bc.ConvertFrom( "#82ccdd" ),
                            RevitColor = new Autodesk.Revit.DB.Color( r, g, b )
                        };
                        break;
                    }
                    case BuiltInCategory.OST_EdgeSlab:
                    {
                        currentModel.FormworkColor = new FormworkColor()
                        {
                            Brush = ( SolidColorBrush )bc.ConvertFrom( "#82ccdd" ),
                            RevitColor = new Autodesk.Revit.DB.Color( r, g, b )
                        };
                        break;
                    }
                }
            }
            catch ( Exception )
            {
            }
        }

        private void dgFormwork_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            try
            {
                //if ( dgFormwork.SelectedItems.Count == 1 )
                //{
                //    ICollection<ElementId> ids = new List<ElementId>();

                //    foreach ( FormworkModel formwork in dgFormwork.SelectedItems )
                //    {
                //        ElementId id = formwork?.FormworkShape?.Id;
                //        if ( FormworkUtil.IsValid( id ) )
                //        {
                //            ids.Add( formwork.FormworkShape.Id );
                //        }
                //    }

                //    if ( ids.Count > 0 )
                //        uidoc.Selection.SetElementIds( ids );
                //}
            }
            catch ( Exception ex )
            {
                return;
                //this.richTxtbox.AppendText(
                //            $"{FormworkUtil.Get_CodeLineNumber( ex )} : " +
                //            $" {ex.Message + Environment.NewLine}" );
            }
        }


        private void btn_header_maximize_Click( object sender, RoutedEventArgs e )
        {
            try
            {
                if ( this.WindowState != WindowState.Maximized )
                {
                    this.WindowState = WindowState.Maximized;
                    this.BorderThickness = new System.Windows.Thickness( 5 );

                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    this.BorderThickness = new System.Windows.Thickness( 0.5 );
                }
            }
            catch ( Exception )
            {
                return;
            }
        }

        private void btn_Close_Click( object sender, RoutedEventArgs e )
        {
            Close();
        }

        private void headerGrid_MouseDown( object sender, MouseButtonEventArgs e )
        {
            if ( e.ChangedButton == System.Windows.Input.MouseButton.Left )
                this.DragMove();
        }

        private void contxt_selectionRevitElem_Click( object sender, RoutedEventArgs e )
        {
            try
            {
                if ( dgFormwork.SelectedItems.Count > 0 )
                {
                    ICollection<ElementId> ids = new List<ElementId>();

                    foreach ( FormworkModel formwork in dgFormwork.SelectedItems )
                    {
                        ElementId id = formwork?.FormworkShape?.Id;
                        if ( FormworkUtil.IsValid( id ) )
                        {
                            ids.Add( formwork.FormworkShape.Id );
                        }
                    }

                    if ( ids.Count > 0 )
                        uidoc.Selection.SetElementIds( ids );
                }
            }
            catch ( Exception )
            {
            }
        }

        private void contxt_deleteRevitElem_Click( object sender, RoutedEventArgs e )
        {
            //MakeRequest( RequestId.DeleteElement );
        }
    }
}
