using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Extensibility;
using ESRI.ArcGIS.Client.Tasks;
using System.Collections;
using System.Diagnostics;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Symbols;

namespace FLR.AddIns
{
    public partial class FLRDialog : UserControl
    {
        // Create a Map property in order to bind the map extent in the XAML
        public Map Map { get; internal set; }
        public String serviceUrl { get; internal set; }
        public String feederUrl { get; internal set; }
        public String towerUrl { get; internal set; }
        public String stationUrl { get; internal set; }
        private Dictionary<string, Graphic> towers = new Dictionary<string, Graphic>();
        private Dictionary<string, Graphic> substations = new Dictionary<string, Graphic>();
        private bool firstHitStation = true;
        List<Graphic> segments = new List<Graphic>();
        Graphic startStation = null;
        private static ESRI.ArcGIS.Client.Projection.WebMercator mercator =
            new ESRI.ArcGIS.Client.Projection.WebMercator();
        private string selectedStation = "";
        private Envelope ext = null;

        public FLRDialog()
        {
            InitializeComponent();
            this.DataContext = this;
            //button1.IsEnabled = false;
        }

        public void loadData()
        {
            feederUrl = serviceUrl.Split(';')[0] + "2";
            towerUrl = serviceUrl.Split(';')[0] + "1";
            stationUrl = serviceUrl.Split(';')[0] + "0";
            QueryTask queryTask = new QueryTask(feederUrl);
            queryTask.DisableClientCaching = true;
            queryTask.ExecuteCompleted += QueryTask_ExecuteCompleted;
            queryTask.Failed += QueryTask_Failed;

            ESRI.ArcGIS.Client.Tasks.Query query = new ESRI.ArcGIS.Client.Tasks.Query();
            // Specify fields to return from initial query
            query.OutFields.AddRange(new string[] { "Feeder_ID", "LineName", "From_Substation", "To_Substation" });

            // This query will just populate the drop-down, so no need to return geometry
            query.ReturnGeometry = false;

            // Return all features
            query.Where = "1=1";
            queryTask.ExecuteAsync(query, "getfeederIDs");
        }

        private void QueryTask_ExecuteCompleted(object sender, ESRI.ArcGIS.Client.Tasks.QueryEventArgs args)
        {
            FeatureSet featureSet = args.FeatureSet;
            //MessageBox.Show(args.UserState as string);
            // If initial query to populate states combo box
            if ((args.UserState as string) == "initial")
            {
                return;
            }
            if ((args.UserState as string) == "getfeederIDs")
            {
                CB_Feeders.Items.Clear();
                CB_Feeders.Items.Add("Select...");

                CB_From_Substation.Items.Clear();
                CB_From_Substation.Items.Add("Select...");

                foreach (Graphic graphic in args.FeatureSet.Features)
                {
                    CB_Feeders.Items.Add(graphic.Attributes["Feeder_ID"].ToString());
                    if (CB_From_Substation.Items.IndexOf(graphic.Attributes["From_Substation"].ToString()) == -1)
                        CB_From_Substation.Items.Add(graphic.Attributes["From_Substation"].ToString());
                    if (CB_From_Substation.Items.IndexOf(graphic.Attributes["To_Substation"].ToString()) == -1)
                        CB_From_Substation.Items.Add(graphic.Attributes["To_Substation"].ToString());
                }

                CB_Feeders.SelectedIndex = 0;
                CB_From_Substation.SelectedIndex = 0;

                QueryTask queryTask = new QueryTask(towerUrl);
                queryTask.DisableClientCaching = true;
                queryTask.ExecuteCompleted += QueryTask_ExecuteCompleted;
                queryTask.Failed += QueryTask_Failed;
                ESRI.ArcGIS.Client.Tasks.Query query = new ESRI.ArcGIS.Client.Tasks.Query();
                query.ReturnGeometry = true;
                query.OutSpatialReference = Map.SpatialReference;
                //query.OutFields.AddRange(new string[] { "Tower" });
                query.OutFields.Add("*");
                // Return all features
                query.Where = "1=1";
                queryTask.ExecuteAsync(query, "gettowers");
                return;
            }

            if ((args.UserState as string) == "getfeedersbystation")
            {
                CB_Feeders.Items.Clear();
                CB_Feeders.Items.Add("Select...");

                foreach (Graphic graphic in args.FeatureSet.Features)
                {
                    CB_Feeders.Items.Add(graphic.Attributes["Feeder_ID"].ToString());
                }

                CB_Feeders.SelectedIndex = 0;
                return;
            }

            if ((args.UserState as string) == "gettowers")
            {
                foreach (Graphic graphic in args.FeatureSet.Features)
                {
                    if (!towers.ContainsKey(graphic.Attributes["Tower"].ToString()))
                        towers.Add(graphic.Attributes["Tower"].ToString(), graphic);
                    //Debug.WriteLine(graphic.Attributes["Tower"].ToString());
                }

                QueryTask queryTask = new QueryTask(stationUrl);
                queryTask.DisableClientCaching = true;
                queryTask.ExecuteCompleted += QueryTask_ExecuteCompleted;
                queryTask.Failed += QueryTask_Failed;
                ESRI.ArcGIS.Client.Tasks.Query query = new ESRI.ArcGIS.Client.Tasks.Query();
                query.ReturnGeometry = true;
                query.OutSpatialReference = Map.SpatialReference;
                query.OutFields.AddRange(new string[] { "Name" });
                // Return all features
                query.Where = "1=1";
                queryTask.ExecuteAsync(query, "getsubstations");

                return;
            }

            if ((args.UserState as string) == "getsubstations")
            {
                foreach (Graphic graphic in args.FeatureSet.Features)
                {
                    if (!substations.ContainsKey(graphic.Attributes["Name"].ToString()))
                        substations.Add(graphic.Attributes["Name"].ToString(), graphic);
                }
            }

            if ((args.UserState as string) == "getonefeeder")
            {
                ESRI.ArcGIS.Client.Geometry.PointCollection pointCollection = new ESRI.ArcGIS.Client.Geometry.PointCollection();
                
                segments = new List<Graphic>();
                Graphic seed = null;
                
                foreach (Graphic graphic in args.FeatureSet.Features)
                {
                    seed = graphic;
                    string towersStr = graphic.Attributes["Towers"].ToString();
                    string sub_st1 = graphic.Attributes["From_Substation"].ToString();
                    string sub_st2 = graphic.Attributes["To_Substation"].ToString();

                    CB_From_Substation.Items.Clear();
                    CB_From_Substation.Items.Add("Select...");
                    CB_From_Substation.Items.Add(sub_st1);
                    CB_From_Substation.Items.Add(sub_st2);
                    if (selectedStation != "")
                    {
                        CB_From_Substation.SelectedItem = selectedStation;
                        if (sub_st1 == selectedStation)
                            LB_ToStation.Content = sub_st2;
                        else
                            LB_ToStation.Content = sub_st1;
                    }
                    else
                        CB_From_Substation.SelectedIndex = 0;

                    string[] towerParts = towersStr.Split(';');
                    MapPoint prevPt = null;
                    string prevTower = "";
                    int i = 0;
                    foreach (string part in towerParts)
                    {
                        
                        Graphic tower = null;
                        MapPoint aPt = null;
                        if (towers.TryGetValue(part, out tower))
                        {
                            aPt = towers[part].Geometry as MapPoint;
                            Debug.WriteLine(part + "," + aPt.X +","+aPt.Y);
                        }
                        else
                        {
                            Debug.WriteLine(part + " is not found.");
                            continue;
                        }
                        pointCollection.Add(aPt);

                        //create line segments for length calculation
                        if (i != 0)
                        {
                            ESRI.ArcGIS.Client.Geometry.PointCollection segPointCollection = new ESRI.ArcGIS.Client.Geometry.PointCollection();
                            segPointCollection.Add(prevPt);
                            segPointCollection.Add(aPt);
                            ESRI.ArcGIS.Client.Geometry.Polyline segment = new ESRI.ArcGIS.Client.Geometry.Polyline();
                            segment.Paths.Add(segPointCollection);
                            segment.SpatialReference = new SpatialReference(102100);
                            Graphic segGraphic = new Graphic()
                            {
                                Geometry = segment,
                            };
                            segGraphic.Attributes.Add("fromTower", prevTower);
                            segGraphic.Attributes.Add("toTower", part);
                            segments.Add(segGraphic);
                        }
                        prevPt = aPt;
                        prevTower = part;
                        i++;
                    }
                }

                GeometryService geometryService =
                            new GeometryService(serviceUrl.Split(';')[1]);
                geometryService.LengthsCompleted += GeometryService_LengthsCompleted;
                geometryService.Failed += GeometryService_Failed;
                geometryService.LengthsAsync(segments, LinearUnit.Foot, true, null);

                ESRI.ArcGIS.Client.Geometry.Polyline polyline = new ESRI.ArcGIS.Client.Geometry.Polyline();
                polyline.Paths.Add(pointCollection);
                Map.Extent = polyline.Extent.Expand(1.5);

                
                // Create the results graphics layer or retrieve the existing one
                GraphicsLayer graphicsLayer = null;
                graphicsLayer = GetOrCreateLayer("Feeder");
                graphicsLayer.ClearGraphics();

                Graphic feederPL = new Graphic()
                {
                    Geometry = polyline
                };
                //feederPL.Attributes.Add("Name", CB_Feeders.SelectedItem as string);
                feederPL.Attributes.Add("ID", (args.FeatureSet.Features[0] as Graphic).Attributes["Feeder_ID"]);
                feederPL.Attributes.Add("Line", (args.FeatureSet.Features[0] as Graphic).Attributes["LineName"]);
                feederPL.Attributes.Add("From", (args.FeatureSet.Features[0] as Graphic).Attributes["From_Substation"]);
                feederPL.Attributes.Add("To", (args.FeatureSet.Features[0] as Graphic).Attributes["To_Substation"]);
                graphicsLayer.Graphics.Add(feederPL);

                if (MapApplication.Current.Map.Layers[graphicsLayer.ID] == null)
                    MapApplication.Current.Map.Layers.Add(graphicsLayer);

                return;
            }
        }

        // Retrieve the layer with the passed in ID from the map.  If it does not exist, create it.
        private GraphicsLayer GetOrCreateLayer(string layerId)
        {
            Layer layer = MapApplication.Current.Map.Layers[layerId];
            if (layer != null && layer is GraphicsLayer)
            {
                return layer as GraphicsLayer;
            }
            else
            {
                GraphicsLayer gLayer = null;
                
                if (layerId == "Fault")
                {
                    gLayer = new GraphicsLayer()
                    {
                        ID = layerId,
                        
                        Renderer = new SimpleRenderer()
                        {
                            Symbol = LayoutRoot.Resources["CustomStrobeMarkerSymbol"] as Symbol
                        }
                    };
                }
                else if (layerId == "Feeder")
                {
                    gLayer = new GraphicsLayer()
                    {
                        ID = layerId,
                        Renderer = new SimpleRenderer()
                        {
                            Symbol = LayoutRoot.Resources["DefaultLineSymbol"] as Symbol
                        }
                    };
                }
                else
                {
                    gLayer = new GraphicsLayer()
                    {
                        ID = layerId
                    };
                }
                gLayer.SetValue(MapApplication.LayerNameProperty, layerId);
                return gLayer;
            }
        }

        private void QueryTask_Failed(object sender, TaskFailedEventArgs args)
        {
            MessageBox.Show("Query failed: " + args.Error);
        }

        private void CB_Feeders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CB_Feeders.SelectedItem == null || CB_Feeders.SelectedItem.ToString().Contains("Select..."))
                return;

            QueryTask queryTask = new QueryTask(feederUrl);
            queryTask.DisableClientCaching = true;
            queryTask.ExecuteCompleted += QueryTask_ExecuteCompleted;
            queryTask.Failed += QueryTask_Failed;

            ESRI.ArcGIS.Client.Tasks.Query query = new ESRI.ArcGIS.Client.Tasks.Query();
            // Specify fields to return from initial query
            query.OutFields.Add("*");

            // This query will just populate the drop-down, so no need to return geometry
            query.ReturnGeometry = true;
            // Return all features
            query.Where = "Feeder_ID='" + CB_Feeders.SelectedItem.ToString()+"'";
            queryTask.ExecuteAsync(query, "getonefeeder");
            firstHitStation = false;
        }

        private void CB_FromStation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CB_From_Substation.SelectedItem == null || CB_From_Substation.SelectedItem.ToString().Contains("Select..."))
                return;
            if (firstHitStation)
                //need to have a reset button to set firstHit to false again
            {
                string substationName = CB_From_Substation.SelectedItem as string;
                selectedStation = substationName;
                QueryTask queryTask = new QueryTask(feederUrl);
                queryTask.DisableClientCaching = true;
                queryTask.ExecuteCompleted += QueryTask_ExecuteCompleted;
                queryTask.Failed += QueryTask_Failed;
                ESRI.ArcGIS.Client.Tasks.Query query = new ESRI.ArcGIS.Client.Tasks.Query();
                query.ReturnGeometry = false;
                query.OutSpatialReference = Map.SpatialReference;
                //query.OutFields.AddRange(new string[] { "Tower" });
                query.OutFields.Add("Feeder_ID");
                // Return all features
                query.Where = "From_substation='" + substationName + "' or To_substation='" + substationName + "'";
                queryTask.ExecuteAsync(query, "getfeedersbystation");
            }
            else
            {
                //just update end station label
            }
           
            
            if (CB_From_Substation.SelectedIndex == 1)
            {
                LB_ToStation.Content = CB_From_Substation.Items[2] as string;
            }
            else if (CB_From_Substation.SelectedIndex == 2)
            {
                LB_ToStation.Content = CB_From_Substation.Items[1] as string;
            }
            else
                return;
            //drawStartStation();
            

        }

        private void drawStartStation()
        {
            GraphicsLayer graphicsLayer = null;
            graphicsLayer = GetOrCreateLayer("Start Substation");

            if (startStation != null)
                graphicsLayer.Graphics.Remove(startStation);
            startStation = substations[CB_From_Substation.SelectedItem as string];
            graphicsLayer.Graphics.Add(startStation);

            // Add the results graphics layer to the map if it has not already been added
            if (MapApplication.Current.Map.Layers[graphicsLayer.ID] == null)
               MapApplication.Current.Map.Layers.Add(graphicsLayer);
        }

        private void drawTowers(string fromTowerID, string toTowerID)
        {
            GraphicsLayer graphicsLayer = null;
            graphicsLayer = GetOrCreateLayer("Adjacent Towers");
            graphicsLayer.ClearGraphics();
            Graphic fromTower = towers[fromTowerID];
            graphicsLayer.Graphics.Add(fromTower);
            Graphic toTower = towers[toTowerID];
            graphicsLayer.Graphics.Add(toTower);

            // Add the results graphics layer to the map if it has not already been added
            if (MapApplication.Current.Map.Layers[graphicsLayer.ID] == null)
                MapApplication.Current.Map.Layers.Add(graphicsLayer);

            ext = new Envelope(fromTower.Geometry as MapPoint, toTower.Geometry as MapPoint);
            //MapApplication.Current.Map.Extent = ext.Expand(1.5);
        }

        private void drawFault(string fromTowerID, string toTowerID, double len, double ratio, double sum)
        {
            Graphic fromTower = towers[fromTowerID];
            Graphic toTower = towers[toTowerID];
            string loc =  fromTower.Attributes["Location"] as string;
            double from_x = (fromTower.Geometry as MapPoint).X;
            double from_y = (fromTower.Geometry as MapPoint).Y;
            double to_x = (toTower.Geometry as MapPoint).X;
            double to_y = (toTower.Geometry as MapPoint).Y;

            double x = from_x + (to_x - from_x) * ratio;
            double y = from_y + (to_y - from_y) * ratio;

            // Create the results graphics layer or retrieve the existing one
            GraphicsLayer graphicsLayer = null;
            graphicsLayer = GetOrCreateLayer("Fault");
            graphicsLayer.ClearGraphics();

            Graphic fault = new Graphic()
            {
                Geometry = new MapPoint(x, y)
            };


            double lat = (mercator.ToGeographic(fault.Geometry) as MapPoint).Y;
            fault.Attributes.Add("Latitude", Math.Round(lat, 6));
            double longi = (mercator.ToGeographic(fault.Geometry) as MapPoint).X;
            fault.Attributes.Add("Longitude", Math.Round(longi, 6));
            fault.Attributes.Add("Location", loc);
            fault.Attributes.Add("From Tower", fromTowerID);
            double fromT = Math.Round(len * ratio, 0);
            double toT = Math.Round(len * (1 - ratio), 0);
            fault.Attributes.Add("Dist_From_Tower_FT", fromT);
            fault.Attributes.Add("To Tower", toTowerID);
            fault.Attributes.Add("Dist_To_Tower_F", toT);
            fault.Attributes.Add("Feeder_Len_FT", Math.Round(sum, 2));
          
            graphicsLayer.Graphics.Add(fault);

            if (MapApplication.Current.Map.Layers[graphicsLayer.ID] == null)
                MapApplication.Current.Map.Layers.Add(graphicsLayer);

            TB_Latlong.Text = Math.Round(longi, 6) + ", " + Math.Round(lat, 6);
            TB_Location.Text = loc;
            TB_FTower.Text = fromTowerID + " (" + fromT + " ft)";
            TB_TTower.Text = toTowerID + " (" + toT + " ft)";
            TB_Feeder_Len.Text = Math.Round(sum, 0) + " ft";

            tabControl1.SelectedIndex = 1;
        }

        private void GeometryService_LengthsCompleted(object sender, ESRI.ArcGIS.Client.Tasks.LengthsEventArgs args)
        {
            //MessageBox.Show(String.Format("The distance of the polyline is {0} miles", Math.Round(args.Results[0], 3)));
            int i = 0;
            foreach (double length in args.Results)
            {
                Graphic segm = segments[i];
                segm.Attributes.Add("length", length);
                i++;
            }
        }

        private void GeometryService_Failed(object sender, TaskFailedEventArgs e)
        {
            MessageBox.Show("Geometry Service error: " + e.Error);
        }

        private void bt_Go_Click(object sender, RoutedEventArgs e)
        {
            double sum = 0;

            if (segments.Count == 0)
            {
                LB_Msg.Content = "Please select a feeder.";
            }
            
            if (TB_Percent.Text == "" && TB_Length.Text == "")
            {
                LB_Msg.Content = "Please enter valid percertage or value.";
                return;
            }

            foreach (Graphic segm in segments) // Loop through List with foreach
            {
                //Debug.WriteLine(segm.Attributes["length"]);
                double len = (double)segm.Attributes["length"];
                Debug.WriteLine(segm.Attributes["fromTower"] + "," + segm.Attributes["toTower"] + "," + len);
                sum += len;
            }
            Debug.WriteLine("Feeder length: "+sum);

            string Str;
            double Num;
            bool isNum;
            double faultLen = 0;
            if (TB_Percent.Text != "")
            {
                Str = TB_Percent.Text.Trim();
                isNum = double.TryParse(Str, out Num);

                if (!isNum)
                {
                    LB_Msg.Content = "Invalid value!";
                    return;
                }

                double perc = Convert.ToDouble(TB_Percent.Text);

                if (perc <= 0 || perc >= 100)
                {
                    LB_Msg.Content = "Invalid value.";
                    return;
                }
                if (CB_From_Substation.SelectedIndex != 1)
                {
                    perc = 100 - perc;
                }
                faultLen = sum * perc / 100;
            }
            else
            {
                Str = TB_Length.Text.Trim();
                isNum = double.TryParse(Str, out Num);

                if (!isNum)
                {
                    LB_Msg.Content = "Invalid value!";
                    return;
                }

                faultLen = double.Parse(TB_Length.Text.Trim());
                if (faultLen > sum)
                {
                    LB_Msg.Content = "Invalid value!";
                    return;
                }

                if (CB_From_Substation.SelectedIndex != 1)
                {
                    faultLen = sum - faultLen;
                }
            }

            LB_Msg.Content = "";
           
            double seg_sum = 0;
            Graphic curSegm = null;
            double distFromTower = 0;
            double ratio = 0;
            double segmlen = 0;
            foreach (Graphic segm in segments) // Loop through List with foreach
            {
                Debug.WriteLine(segm.Attributes["length"]);
                curSegm = segm;
                segmlen = (double)segm.Attributes["length"];
                seg_sum += segmlen;

                if (seg_sum > faultLen)
                {
                    distFromTower = segmlen - (seg_sum - faultLen);
                    ratio = distFromTower / segmlen;
                    break;
                }
            }

            drawTowers(curSegm.Attributes["fromTower"] as string, curSegm.Attributes["toTower"] as string);
            drawFault(curSegm.Attributes["fromTower"] as string, curSegm.Attributes["toTower"] as string, segmlen, ratio, sum);
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            firstHitStation = true;
            selectedStation = "";
            QueryTask queryTask = new QueryTask(feederUrl);
            queryTask.DisableClientCaching = true;
            queryTask.ExecuteCompleted += QueryTask_ExecuteCompleted;
            queryTask.Failed += QueryTask_Failed;

            ESRI.ArcGIS.Client.Tasks.Query query = new ESRI.ArcGIS.Client.Tasks.Query();
            // Specify fields to return from initial query
            query.OutFields.AddRange(new string[] { "Feeder_ID", "LineName", "From_Substation", "To_Substation" });

            // This query will just populate the drop-down, so no need to return geometry
            query.ReturnGeometry = false;

            // Return all features
            query.Where = "1=1";
            queryTask.ExecuteAsync(query, "getfeederIDs");

            segments = new List<Graphic>();
            TB_Length.Text = "";
            TB_Percent.Text = "";

            GraphicsLayer graphicsLayer = null;
            graphicsLayer = GetOrCreateLayer("Fault");
            graphicsLayer.ClearGraphics();

            graphicsLayer = GetOrCreateLayer("Adjacent Towers");
            graphicsLayer.ClearGraphics();

            graphicsLayer = GetOrCreateLayer("Feeder");
            graphicsLayer.ClearGraphics();
            TB_Latlong.Text = "";
            TB_Location.Text = "";
            TB_FTower.Text = "";
            TB_TTower.Text = "";
            LB_ToStation.Content = "";
            ext = null;
        }

        private void TB_Percent_TextChanged(object sender, TextChangedEventArgs e)
        {
            TB_Length.Text = "";
        }

        private void TB_Length_TextChanged(object sender, TextChangedEventArgs e)
        {
            TB_Percent.Text = "";
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            if (ext == null)
                return;

            MapApplication.Current.Map.Extent = ext.Expand(1.5);
        }
    }
}
