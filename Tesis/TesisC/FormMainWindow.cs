using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gma.CodeCloud.Controls;
using Gma.CodeCloud.Controls.Geometry;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System.Timers;
using System.Windows.Forms.DataVisualization.Charting;

namespace TesisC
{
    public partial class FormMainWindow : Form
    {
        private Core core;
        private BackgroundWorker worker, cloudWorker;

        private CloudControl cloud;
        private bool buildingCloud; // Para no tocar la db

        private int sortByCol = 2;

        private DbTopic ActiveTopic = null;
        private List<KeyValuePair<DbTopic, AnalysisResults>> resList;

        private PointLatLng Argentina;

        private System.Timers.Timer ifaceTimer;

        public FormMainWindow()
        {
            InitializeComponent();

            #region CLOUD CONTROL
            try
            {
                cloud = new CloudControl();
                this.cloud.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                            | System.Windows.Forms.AnchorStyles.Left)
                            | System.Windows.Forms.AnchorStyles.Right)));
                this.cloud.BorderStyle = System.Windows.Forms.BorderStyle.None;
                this.cloud.Font = new System.Drawing.Font("Verdana", 8, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                this.cloud.LayoutType = Gma.CodeCloud.Controls.LayoutType.Spiral;
                this.cloud.Location = new System.Drawing.Point(16, 16);
                this.cloud.BackColor = Color.Beige;
                this.cloud.MaxFontSize = 24;
                this.cloud.MinFontSize = 8;
                this.cloud.Name = "cloudControl";
                this.cloud.Palette = new System.Drawing.Color[] {  System.Drawing.Color.LightGray};
                this.cloud.Size = new System.Drawing.Size(484, 500);
                this.cloud.TabIndex = 6;
                this.cloud.WeightedWords = null;
                this.cloud.Click += this.CloudControlClick;
                this.cloud.Enabled = false;
                
                // this.cloud.Click += new System.EventHandler();

                this.Controls.Add(cloud);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
            }
            #endregion
            buildingCloud = false;

            core = new Core();
            core.InitListen();

            Type tp = tableLayoutPanel1.GetType().BaseType;
            System.Reflection.PropertyInfo pi =
                tp.GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic);
            pi.SetValue(tableLayoutPanel1, true, null);

            // Map
            gMapControl1.MapProvider = GMap.NET.MapProviders.BingHybridMapProvider.Instance;
            gMapControl1.Zoom = 4;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;

            Argentina = new PointLatLng(-40.4, -63.6);
            gMapControl1.Position = Argentina;
            gMapControl1.Update();

            cloudWorker = new BackgroundWorker();

            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(core.StartStream);
            worker.RunWorkerCompleted += workCompleted;
            worker.RunWorkerAsync();

            ifaceTimer = new System.Timers.Timer();
            ifaceTimer.Interval = core.TS_quick.TotalMilliseconds;
            ifaceTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnIFaceTimer);
            ifaceTimer.SynchronizingObject = this;
            ifaceTimer.Start();

            this.comboBoxTime.SelectedIndex = 0;
            UpdateIface();
        }

        private void OnIFaceTimer(object source, ElapsedEventArgs e)
        {
            Console.Out.WriteLine("\n-- IFACE TIMER --");
            UpdateIface();
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void workCompleted(object sender, RunWorkerCompletedEventArgs e)
        {               
            worker.RunWorkerAsync();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            UpdateIface();
        }

        private void UpdateIface()
        {
            if (!buildingCloud)
            {
                tableLayoutPanel1.Enabled = false;
                Dictionary<DbTopic, AnalysisResults> res = core.GetTopicsData();
                resList = res.ToList();
                UpdateTable();
                tableLayoutPanel1.Enabled = true;
                UpdateMap();
                UpdateChart();
            }
        }

        private void UpdateChart()
        {
            try
            {
                chart1.Series.Clear();

                int topicN = 0;
                foreach (DbTopic t in core.GetTopics())
                {
                    chart1.Series.Add(t.Alias[1]);
                    chart1.Series[t.Alias[1]].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
                    chart1.Series[t.Alias[1]].BorderWidth = 2;
                    chart1.Series[t.Alias[1]].XValueType = ChartValueType.DateTime;
                    if (topicN > 10) 
                        chart1.Series[t.Alias[1]].BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash;
                    topicN++;
                }               
               
                int intervalsToShow = 30;
                TimeSpan interval = TimeSpan.FromSeconds(0);

                IEnumerable<DbTimeBlock> tbs = null;
                if(comboBoxTime.SelectedIndex == 0)
                    interval = core.TS_quick;
                else if(comboBoxTime.SelectedIndex == 1)
                    interval = core.TS_short;
                else if (comboBoxTime.SelectedIndex == 2)
                    interval = core.TS_medium;
                else if (comboBoxTime.SelectedIndex == 3)
                    interval = core.TS_long;

                tbs = core.GetTimeBlocks(interval, intervalsToShow);

                //DateTime graphTime = DateTime.Now.Add(TimeSpan.FromTicks(-(intervalsToShow-1) * interval.Ticks));

                chart1.ChartAreas[0].AxisX.LabelStyle.Format = "MM-dd|HH:mm";
                //chart1.ChartAreas[0].AxisX.X .XValueType = ChartValueType.DateTime;
                //chart1.ChartAreas[0].AxisX.IntervalAutoMode = IntervalAutoMode.;

                //chart1.ChartAreas[0].AxisX.Maximum = DateTime.Now;
                //chart1.ChartAreas[0].AxisX.Minimum = -tbs.Count() + 1;

                foreach (DbTimeBlock tb in tbs)
                {
                    foreach (DbTopic t in core.GetTopics())
                    {
                        if (tb.TopicAR.ContainsKey(t))
                        {
                            if (sortByCol == 2)
                                chart1.Series[t.Alias[1]].Points.AddXY(tb.Start, tb.TopicAR[t].Popularity);
                            if (sortByCol == 3)
                                chart1.Series[t.Alias[1]].Points.AddXY(tb.Start, tb.TopicAR[t].PosVal);
                            if (sortByCol == 4)
                                chart1.Series[t.Alias[1]].Points.AddXY(tb.Start, tb.TopicAR[t].NegVal);
                            if (sortByCol == 5)
                                chart1.Series[t.Alias[1]].Points.AddXY(tb.Start, tb.TopicAR[t].Ambiguity);
                        }
                    }
                    //graphTime = graphTime.Add(interval);
                }

            }
            catch (Exception e)
            {
                Console.Out.WriteLine("\n@Chart: fallo al cargar datos o dibujar gráfico\n");
            }

        }

        private void UpdateMap()
        {
            gMapControl1.Overlays.Clear();
            GMapOverlay markersOverlay = new GMapOverlay("markers");
            this.gMapControl1.Overlays.Add(markersOverlay);

            IEnumerable<DbTweet> tws = core.GetGeolocatedTweets(10);

            foreach (DbTweet tw in tws)
            {
                try
                {
                    Console.Out.WriteLine(">>> MAP: " + tw.Coord.Item1 + ", " + tw.Coord.Item2);
                    //gMapControl1.Visible = false;
                    GMarkerGoogle marker = null;

                    if (tw.Coord != null)
                    {
                        marker = new GMarkerGoogle(new PointLatLng(tw.Coord.Item1, tw.Coord.Item2), new Bitmap(core.GetTopicImage(0, core.GetDbTopicFromId(tw.About[0]))));
                        marker.ToolTip = new GMapToolTip(marker);
                        marker.ToolTip.Font = new Font(FontFamily.GenericSansSerif, 8);
                        marker.ToolTipText = tw.Author + "\nen " + tw.Place + "\nPos: " + tw.PosValue + "\nNeg: " + tw.NegValue + "\n\n";
                        String twText = tw.Text;
                        while (twText.Length > 20)
                        {
                            marker.ToolTipText += twText.Substring(0, 20)+ "\n";
                            twText = twText.Substring(20);
                        }
                        marker.ToolTipText += twText;
                    }

                    markersOverlay.Markers.Add(marker);
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine("Problema al agregar marker: " + e.Message + "\n"+e.StackTrace);
                }
            }

            this.gMapControl1.Overlays.Add(markersOverlay);
            //gMapControl1.Visible = true;
            gMapControl1.Update();           
        }


        private void UpdateTable()
        {            
            if (sortByCol == 2) resList.Sort((pair1, pair2) => { return -pair1.Value.Popularity.CompareTo(pair2.Value.Popularity); }); // Pop desc
            if (sortByCol == 3) resList.Sort((pair1, pair2) => { return -pair1.Value.PosVal.CompareTo(pair2.Value.PosVal); }); // Pop desc
            if (sortByCol == 4) resList.Sort((pair1, pair2) => { return -pair1.Value.NegVal.CompareTo(pair2.Value.NegVal); }); // Pop desc
            if (sortByCol == 5) resList.Sort((pair1, pair2) => { return -pair1.Value.Ambiguity.CompareTo(pair2.Value.Ambiguity); }); // Pop desc

            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel1.RowCount = 0;

            tableLayoutPanel1.Controls.Add(new Label() { Text = "Nombre", Width = 200 }, 1, 0);

            Button bPop = new Button() { Text = "Pop.", Width = 200 };
            bPop.Click += (sender, args) =>
            {
                sortByCol = 2;
                UpdateTable();
                UpdateChart();
            };
            tableLayoutPanel1.Controls.Add(bPop, 2, 0);

            Button bPos = new Button() { Text = "+", Width = 200 };
            bPos.Click += (sender, args) =>
            {
                sortByCol = 3;
                UpdateTable();
                UpdateChart();
            };
            tableLayoutPanel1.Controls.Add(bPos, 3, 0);

            Button bNeg = new Button() { Text = "-", Width = 200 };
            bNeg.Click += (sender, args) =>
            {
                sortByCol = 4;
                UpdateTable();
                UpdateChart();
            };
            tableLayoutPanel1.Controls.Add(bNeg, 4, 0);

            Button bAmb = new Button() { Text = "Ambig.", Width = 200 };
            bAmb.Click += (sender, args) =>
            {
                sortByCol = 5;
                UpdateTable();
                UpdateChart();
            };
            tableLayoutPanel1.Controls.Add(bAmb, 5, 0);

            int row = 1;
            foreach (var item in resList)
            {
                Button bName = new Button() { Text = item.Key.Alias[1], Width = 200 };
                bName.Click += (sender, args) => {

                    buildingCloud = true;
                    ActiveTopic = item.Key;
                    cloud.Enabled = false;
                    cloudWorker.Dispose();
                    cloudWorker = new BackgroundWorker();
                    cloudWorker.DoWork += (e, a) =>
                    {
                        try
                        {
                            List<Gma.CodeCloud.Controls.TextAnalyses.Processing.IWord> iwords = new List<Gma.CodeCloud.Controls.TextAnalyses.Processing.IWord>();

                            int cantCloudWords = 0;
                            
                            // Tomo las palabras relevantes de un tópico porque al armar la tabla sólo había pedido un análisis simple.
                            List<KeyValuePair<string,double>> relevantList = core.GetTopicTermIntersectionAnalysis(item.Key, "", true).relevantList;
                            foreach (var i in relevantList)
                            {
                                if (cantCloudWords >  30) break;

                                // Si la palabra es el topic de la nube, la saltea.
                                bool cont = false;
                                foreach (String al in item.Key.Alias)
                                    if (al.Contains(i.Key))
                                        cont = true;
                                if (cont) continue;

                                AnalysisResults intersection = core.GetTopicTermIntersectionAnalysis(item.Key, i.Key, false);
                                int neg;
                                if (intersection == null) neg = 0;
                                else neg = intersection.NegVal;
                                int pos;
                                if (intersection == null) pos = 0;
                                else pos = intersection.PosVal;

                                double rr = (double)(neg - pos) / (neg + pos + 1); int r = (int)(rr * 100) + 127;
                                double gg = (double)(pos - neg) / (neg + pos + 1); int g = (int)(gg * 100) + 127;
                                int b;
                                if (neg == 0 && pos == 0) b = 150;
                                else b = 50;

                                Color c = Color.FromArgb(255, r, g, b);

                                iwords.Add(new Gma.CodeCloud.Controls.TextAnalyses.Processing.Word(i.Key, (int)i.Value, c));
                                cantCloudWords++;
                            }
                            if (iwords.Count > 0) this.cloud.WeightedWords = iwords;
                        }
                        catch (Exception ee)
                        {
                            Console.Out.WriteLine("No se pudo crear nube de palabras. Probablemente no sean suficientes.");
                        }

                        pictureBox1.Image = core.GetTopicImage(2, item.Key);
                    };
                    cloudWorker.RunWorkerCompleted += (e, a) => {
                        
                        cloud.Update();
                        cloud.Enabled = true;
                        buildingCloud = false;
                    };
                    cloudWorker.RunWorkerAsync();
                };
                tableLayoutPanel1.Controls.Add(bName, 1, row);

                Button bTopicPop = new Button() { Text = "" + item.Value.Popularity };
                bTopicPop.Click += (a, e) =>
                {
                    IEnumerable<DbTweet> tweetSet = core.GetTopicTermIntersectionTweets(item.Key, "");   // Funciona intersección con nada       

                    FormTweets tweetsWindow = new FormTweets(core, tweetSet, item.Key, FormTweets.SelectionFav, null);
                    tweetsWindow.Show(); 
                };
                tableLayoutPanel1.Controls.Add(bTopicPop, 2, row);

                Button bTopicPos = new Button() { Text = "" + item.Value.PosVal };
                bTopicPos.Click += (a, e) =>
                {
                    IEnumerable<DbTweet> tweetSet = core.GetTopicTermIntersectionTweets(item.Key, "");         

                    FormTweets tweetsWindow = new FormTweets(core, tweetSet, item.Key, FormTweets.SelectionPos, null);
                    tweetsWindow.Show();
                };
                tableLayoutPanel1.Controls.Add(bTopicPos, 3, row);

                Button bTopicNeg = new Button() { Text = "" + item.Value.NegVal };
                bTopicNeg.Click += (a, e) =>
                {
                    IEnumerable<DbTweet> tweetSet = core.GetTopicTermIntersectionTweets(item.Key, "");       

                    FormTweets tweetsWindow = new FormTweets(core, tweetSet, item.Key, FormTweets.SelectionNeg, null);
                    tweetsWindow.Show();
                };
                tableLayoutPanel1.Controls.Add(bTopicNeg, 4, row);

                Button bTopicAmb = new Button() { Text = "" + (int)(item.Value.Ambiguity * 100) + "%" };
                bTopicAmb.Click += (a, e) =>
                {
                    IEnumerable<DbTweet> tweetSet = core.GetTopicTermIntersectionTweets(item.Key, "");         

                    FormTweets tweetsWindow = new FormTweets(core, tweetSet, item.Key, FormTweets.SelectionAmb, null);
                    tweetsWindow.Show();
                };
                tableLayoutPanel1.Controls.Add(bTopicAmb, 5, row);

                row++;
            }

            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.Update();

            //graphTime++;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            core.Dispose();
        }


        private void CloudControlClick(object sender, EventArgs e)
        {
            LayoutItem itemUnderMouse;
            Point mousePositionRelativeToControl = cloud.PointToClient(new Point(MousePosition.X, MousePosition.Y));
            if (!cloud.TryGetItemAtLocation(mousePositionRelativeToControl, out itemUnderMouse))
            {
                return;
            }

            String w = itemUnderMouse.Word.Text;
            IEnumerable<DbTweet> tweetSet = core.GetTopicTermIntersectionTweets(ActiveTopic, w);            

            FormTweets tweetsWindow = new FormTweets(core, tweetSet, ActiveTopic, FormTweets.SelectionWordRelated, w);
            tweetsWindow.Show(); 
        }

        private void ButtonPurgeClick(object sender, EventArgs e)
        {
            core.PurgeDB();
        }

        private void comboBoxTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateChart();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            core.PurgeDB();
            UpdateIface();
        }

       
    }

}
