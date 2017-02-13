using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;



namespace MarsRover
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        SolidColorBrush myWarningBrush = new SolidColorBrush();

        public String[] preDefinedLocations = new String[] { "29.564769,-95.081271", "39.646101,-79.972755", "39.653846,-79.947039" };


        public MainWindow()
        {
            InitializeComponent();

            myWarningBrush.Color = System.Windows.Media.Color.FromRgb(255, 209, 0);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            configPlannerMap();
        }

        #region Rover Control

            #region Rover Click Events
       
                private void roverListenBtn_Click(object sender, RoutedEventArgs e)
                {
                    //TODO implement connection to server... until then, this is simulated...
                    
                    roverControlBtn.IsEnabled = true;
                    manualControlRB.IsEnabled = true;
                    disconnectRoverBtn.IsEnabled = true;
                    liveTab.IsEnabled = true;

                    connectionStatusLbl.Content = "Connected - No Control";
                    connectionStatusLbl.Foreground = myWarningBrush;
                }

                private void roverControlBtn_Click(object sender, RoutedEventArgs e)
                {

                }

            #endregion
        #endregion

        #region Mission Planner Map

        private void configPlannerMap()
        {
            //Map Config
            missionPlanner.MapProvider = GMap.NET.MapProviders.GoogleSatelliteMapProvider.Instance;
            missionPlanner.MaxZoom = 20;
            missionPlanner.MinZoom = 10;

            //Map Events
            // map events
            missionPlanner.OnPositionChanged += new PositionChanged(missionPlanner_OnCurrentPositionChanged);
            missionPlanner.OnTileLoadStart += new TileLoadStart(missionPlanner_OnTileLoadStart);
            missionPlanner.OnTileLoadComplete += new TileLoadComplete(missionPlanner_OnTileLoadComplete);
            //missionPlanner.OnMarkerClick += new MarkerClick(MainMap_OnMarkerClick);
            missionPlanner.OnMapTypeChanged += new MapTypeChanged(missionPlanner_OnMapTypeChanged);
            missionPlanner.MouseMove += new System.Windows.Forms.MouseEventHandler(missionPlanner_MouseMove);
            missionPlanner.MouseDown += new System.Windows.Forms.MouseEventHandler(missionPlanner_MouseDown);
            missionPlanner.MouseUp += new System.Windows.Forms.MouseEventHandler(missionPlanner_MouseUp);
            //missionPlanner.OnMarkerEnter += new MarkerEnter(MainMap_OnMarkerEnter);
            //missionPlanner.OnMarkerLeave += new MarkerLeave(MainMap_OnMarkerLeave);

            missionPlanner.MapScaleInfoEnabled = false;
            missionPlanner.ScalePen = new System.Drawing.Pen(System.Drawing.Color.Red);

            missionPlanner.DisableFocusOnMouseEnter = true;

            missionPlanner.ForceDoubleBuffer = false;

            missionPlanner.RoutesEnabled = true;

            try
            {
                updateLocation(1);

                missionPlanner.Zoom = 19;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
        }

        #region Text Change Events

        private void latTxt_TextChanged(object sender, TextChangedEventArgs e)
            {
                updateLocation(2);
            }

            private void lonTxt_TextChanged(object sender, TextChangedEventArgs e)
            {
                updateLocation(2);
            }

        #endregion

        #region Update Functions

            
            private void updateLocation(int type)
            {
                if (type == 1) //Selection came from the predifined list
                {
                    if (missionPlanner != null)
                    {
                        missionPlanner.Position = new PointLatLng(double.Parse(preDefinedLocations[locationCombo.SelectedIndex].Substring(0, preDefinedLocations[locationCombo.SelectedIndex].IndexOf(","))),
                               double.Parse(preDefinedLocations[locationCombo.SelectedIndex].Substring(preDefinedLocations[locationCombo.SelectedIndex].IndexOf(",") + 1)));
                        
                        latTxt.TextChanged -= latTxt_TextChanged;
                        lonTxt.TextChanged -= lonTxt_TextChanged;

                        latTxt.Text = preDefinedLocations[locationCombo.SelectedIndex].Substring(0, preDefinedLocations[locationCombo.SelectedIndex].IndexOf(","));
                        lonTxt.Text = preDefinedLocations[locationCombo.SelectedIndex].Substring(preDefinedLocations[locationCombo.SelectedIndex].IndexOf(",") + 1);

                        latTxt.TextChanged += latTxt_TextChanged;
                        lonTxt.TextChanged += lonTxt_TextChanged;
                    }
                }
                else if (type == 2) //User entered coords
                {
                    if (!(latTxt.Text.Equals("") || lonTxt.Text.Equals("") || lonTxt.Text.Equals("-") ||
                        latTxt.Text.Equals("-") || lonTxt.Text.Equals(".") || latTxt.Text.Equals(".") ||
                        lonTxt.Text.Equals("-.") || latTxt.Text.Equals("-.")))
                    {
                        missionPlanner.Position = new PointLatLng(double.Parse(latTxt.Text), double.Parse(lonTxt.Text));
                    }
                }
            }

        #endregion

        #region Selection Events

        private void location_Selection(object sender, SelectionChangedEventArgs e)
            {
                System.Windows.Controls.ComboBox sel = (System.Windows.Controls.ComboBox)sender;

                if (missionPlanner != null)
                {
                    updateLocation(1);
                }
            }

            #endregion

        #region Map Events

            GMapMarker center = new GMarkerGoogle(new PointLatLng(0.0, 0.0), GMarkerGoogleType.none);

            bool isMouseDown = false;
            bool isMouseDraging = false;

            PointLatLng MouseDownStart;
            internal PointLatLng MouseDownEnd;

            void missionPlanner_OnMapTypeChanged(GMapProvider type)
            {
                //comboBoxMapType.SelectedItem = missionPlanner.MapProvider;

                //trackBar1.Minimum = MainMap.MinZoom;
                //trackBar1.Maximum = MainMap.MaxZoom + 0.99f;

                missionPlanner.ZoomAndCenterMarkers("objects");

                /*if (type == WMSProvider.Instance)
                {
                    string url = "";
                    if (MainV2.config["WMSserver"] != null)
                        url = MainV2.config["WMSserver"].ToString();
                    if (System.Windows.Forms.DialogResult.Cancel == InputBox.Show("WMS Server", "Enter the WMS server URL", ref url))
                        return;

                    string szCapabilityRequest = url + "?version=1.1.0&Request=GetCapabilities";

                    XmlDocument xCapabilityResponse = MakeRequest(szCapabilityRequest);
                    ProcessWmsCapabilitesRequest(xCapabilityResponse);

                    MainV2.config["WMSserver"] = url;
                    WMSProvider.CustomWMSURL = url;
                } */
            }

            void missionPlanner_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
            {
                MouseDownEnd = missionPlanner.FromLocalToLatLng(e.X, e.Y);

                if (e.Button == MouseButtons.Right) // ignore right clicks
                {
                    return;
                }

                if (isMouseDown) // mouse down on some other object and dragged to here.
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        isMouseDown = false;
                    }
                    if (!isMouseDraging)
                    {
                        /* if (CurentRectMarker != null)
                         {
                             // cant add WP in existing rect
                         }
                         else
                         {
                             AddWPToMap(currentMarker.Position.Lat, currentMarker.Position.Lng, 0);
                         } */
                    }
                    else
                    {
                        /* if (CurentRectMarker != null)
                         {
                             if (CurentRectMarker.InnerMarker.Tag.ToString().Contains("grid"))
                             {
                                 try
                                 {
                                     drawnpolygon.Points[int.Parse(CurentRectMarker.InnerMarker.Tag.ToString().Replace("grid", "")) - 1] = new PointLatLng(MouseDownEnd.Lat, MouseDownEnd.Lng);
                                     MainMap.UpdatePolygonLocalPosition(drawnpolygon);
                                     MainMap.Invalidate();
                                 }
                                 catch { }
                             }
                             else
                             {
                                 callMeDrag(CurentRectMarker.InnerMarker.Tag.ToString(), currentMarker.Position.Lat, currentMarker.Position.Lng, -1);
                             }
                             CurentRectMarker = null;
                         } */
                    }
                }

                isMouseDraging = false;
            }

            void missionPlanner_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
            {
                MouseDownStart = missionPlanner.FromLocalToLatLng(e.X, e.Y);

                if (e.Button == MouseButtons.Left && System.Windows.Forms.Control.ModifierKeys != Keys.Alt)
                {
                    isMouseDown = true;
                    isMouseDraging = false;

                    /*if (currentMarker.IsVisible)
                    {
                        currentMarker.Position = MainMap.FromLocalToLatLng(e.X, e.Y);
                    } */
                }
            }

            // move current marker with left holding
            void missionPlanner_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
            {
                PointLatLng point = missionPlanner.FromLocalToLatLng(e.X, e.Y);

                if (MouseDownStart == point)
                    return;

                // currentMarker.Position = point;

                if (!isMouseDown)
                {
                    // update mouse pos display
                    SetMouseDisplay(point.Lat, point.Lng, 0);
                }

                //draging
                if (e.Button == MouseButtons.Left && isMouseDown)
                {
                    isMouseDraging = true;
                    /* if (CurrentRallyPt != null)
                     {
                         PointLatLng pnew = MainMap.FromLocalToLatLng(e.X, e.Y);

                         CurrentRallyPt.Position = pnew;
                     }
                     else if (CurentRectMarker != null) // left click pan
                     {
                         try
                         {
                             // check if this is a grid point
                             if (CurentRectMarker.InnerMarker.Tag.ToString().Contains("grid"))
                             {
                                 drawnpolygon.Points[int.Parse(CurentRectMarker.InnerMarker.Tag.ToString().Replace("grid", "")) - 1] = new PointLatLng(point.Lat, point.Lng);
                                 MainMap.UpdatePolygonLocalPosition(drawnpolygon);
                                 MainMap.Invalidate();
                             }
                         }
                         catch { }

                         PointLatLng pnew = MainMap.FromLocalToLatLng(e.X, e.Y);

                         // adjust polyline point while we drag
                         try
                         {
                             int? pIndex = (int?)CurentRectMarker.Tag;
                             if (pIndex.HasValue)
                             {
                                 if (pIndex < wppolygon.Points.Count)
                                 {
                                     wppolygon.Points[pIndex.Value] = pnew;
                                     lock (thisLock)
                                     {
                                         MainMap.UpdatePolygonLocalPosition(wppolygon);
                                     }
                                 }
                             }
                         }
                         catch { }

                         // update rect and marker pos.
                         if (currentMarker.IsVisible)
                         {
                             currentMarker.Position = pnew;
                         }
                         CurentRectMarker.Position = pnew;

                         if (CurentRectMarker.InnerMarker != null)
                         {
                             CurentRectMarker.InnerMarker.Position = pnew;
                         }
                     }
                     else // left click pan
                     {
                         double latdif = MouseDownStart.Lat - point.Lat;
                         double lngdif = MouseDownStart.Lng - point.Lng;

                         try
                         {
                             lock (thisLock)
                             {
                                 MainMap.Position = new PointLatLng(center.Position.Lat + latdif, center.Position.Lng + lngdif);
                             }
                         }
                         catch { }
                     } */
                }
            }

            // MapZoomChanged
            void missionPlanner_OnMapZoomChanged()
            {
                if (missionPlanner.Zoom > 0)
                {
                    try
                    {
                        //trackBar1.Value = (int)(MainMap.Zoom);
                    }
                    catch { }
                    //textBoxZoomCurrent.Text = MainMap.Zoom.ToString();
                    center.Position = missionPlanner.Position;
                }
            }

            // loader start loading tiles
            void missionPlanner_OnTileLoadStart()
            {
                MethodInvoker m = delegate()
                {
                    //lbl_status.Text = "Status: loading tiles...";
                };
                try
                {
                    //BeginInvoke(m);
                }
                catch
                {
                }
            }

            // loader end loading tiles
            void missionPlanner_OnTileLoadComplete(long ElapsedMilliseconds)
            {

                //MainMap.ElapsedMilliseconds = ElapsedMilliseconds;

                MethodInvoker m = delegate()
                {
                    //lbl_status.Text = "Status: loaded tiles";
                };
                try
                {
                    //if (!this.IsDisposed)
                    //BeginInvoke(m);
                }
                catch
                {
                }

            }

            // current point changed
            void missionPlanner_OnCurrentPositionChanged(PointLatLng point)
            {
                if (point.Lat > 90) { point.Lat = 90; }
                if (point.Lat < -90) { point.Lat = -90; }
                if (point.Lng > 180) { point.Lng = 180; }
                if (point.Lng < -180) { point.Lng = -180; }
                center.Position = point;
                //TXT_mouselat.Text = point.Lat.ToString(CultureInfo.InvariantCulture);
                //TXT_mouselong.Text = point.Lng.ToString(CultureInfo.InvariantCulture);
            }

            PointLatLngAlt mouseposdisplay = new PointLatLngAlt(0, 0);

            /// <summary>
            /// Used for current mouse position
            /// </summary>
            /// <param name="lat"></param>
            /// <param name="lng"></param>
            /// <param name="alt"></param>
            public void SetMouseDisplay(double lat, double lng, int alt)
            {
                mouseposdisplay.Lat = lat;
                mouseposdisplay.Lng = lng;
                //mouseposdisplay.Alt = alt;

                //TXT_mousealt.Text = srtm.getAltitude(mouseposdisplay.Lat, mouseposdisplay.Lng, MainMap.Zoom).ToString("0");
                plannerMouseLocation.Content = mouseposdisplay.Lat.ToString("0.######") + "," + mouseposdisplay.Lng.ToString("0.######");

                //int zone = mouseposdisplay.GetUTMZone();

                //txt_mouse_utmx.Text = mouseposdisplay.ToUTM(zone)[0].ToString("#.###");
                //txt_mouse_utmy.Text = mouseposdisplay.ToUTM(zone)[1].ToString("#.###");
                //txt_mouse_utmzone.Text = zone.ToString("0N;0S");

                //if (Math.Abs(lat) < 80)
                //{
                //    txt_mouse_mgrs.Text = mouseposdisplay.GetMGRS();
                //}
                //else
                //{
                //    txt_mouse_mgrs.Text = "Invalid";
                //}

                /*try
                {
                    double lastdist = MainMap.MapProvider.Projection.GetDistance(wppolygon.Points[wppolygon.Points.Count - 1], currentMarker.Position);

                    lbl_prevdist.Text = rm.GetString("lbl_prevdist.Text") + ": " + FormatDistance(lastdist, true);

                    double homedist = MainMap.MapProvider.Projection.GetDistance(currentMarker.Position, wppolygon.Points[0]);

                    lbl_homedist.Text = rm.GetString("lbl_homedist.Text") + ": " + FormatDistance(homedist, true);
                }
                catch { } */
            }

            #endregion     

            

            
    #endregion

           
    }
}
