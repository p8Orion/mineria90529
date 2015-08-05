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

namespace TesisC
{
    public partial class FormMainWindow : Form
    {
        private Core core;
        private BackgroundWorker worker, cloudWorker;

        private CloudControl cloud;

        private int graphTime;
        private int sortByCol = 2;

        private DbTopic ActiveTopic = null;
        private List<KeyValuePair<DbTopic, AnalysisResults>> resList;

        private bool userMode = false;

        public FormMainWindow()
        {
            InitializeComponent();
            this.tableLayoutPanel1.Enabled = false;

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
                this.cloud.Location = new System.Drawing.Point(0, 0);
                this.cloud.BackColor = Color.Beige;
                this.cloud.MaxFontSize = 32;
                this.cloud.MinFontSize = 8;
                this.cloud.Name = "cloudControl";
                this.cloud.Palette = new System.Drawing.Color[] {  System.Drawing.Color.LightGray};
                this.cloud.Size = new System.Drawing.Size(500, 500);
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

            core = new Core();
            core.InitListen();

            Type tp = tableLayoutPanel1.GetType().BaseType;
            System.Reflection.PropertyInfo pi =
                tp.GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic);
            pi.SetValue(tableLayoutPanel1, true, null);

            graphTime = 0;

            cloudWorker = new BackgroundWorker();

            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(core.StartStream);
            worker.RunWorkerCompleted += workCompleted;
            worker.RunWorkerAsync();

            UpdateData();
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (userMode)
            {
                userMode = false;
                cloud.Enabled = false;
                tableLayoutPanel1.Enabled = false;
                button1.Text = "Actualizando datos. Click para realizar consultas.";
                worker.RunWorkerAsync();
                UpdateData();
            }
            else if (!userMode)
            {
                button1.Enabled = false;
                button1.Text = "(Esperando finalización de consulta)";
                userMode = true;
            }
        }

        private void workCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (userMode)
            {
                button1.Enabled = true;
                cloud.Enabled = true;
                tableLayoutPanel1.Enabled = true;
                button1.Text = "Click para volver a actualizar datos.";
                UpdateData();
            }
            else
            {
                worker.RunWorkerAsync();
                UpdateData();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            UpdateData();
        }

        private void UpdateData()
        {
            Dictionary<DbTopic, AnalysisResults> res = core.GetTopicsData();
            resList = res.ToList();
            UpdateTable();
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
            };
            tableLayoutPanel1.Controls.Add(bPop, 2, 0);

            Button bPos = new Button() { Text = "+", Width = 200 };
            bPos.Click += (sender, args) =>
            {
                sortByCol = 3;
                UpdateTable();
            };
            tableLayoutPanel1.Controls.Add(bPos, 3, 0);

            Button bNeg = new Button() { Text = "-", Width = 200 };
            bNeg.Click += (sender, args) =>
            {
                sortByCol = 4;
                UpdateTable();
            };
            tableLayoutPanel1.Controls.Add(bNeg, 4, 0);

            Button bAmb = new Button() { Text = "Ambig.", Width = 200 };
            bAmb.Click += (sender, args) =>
            {
                sortByCol = 5;
                UpdateTable();
            };
            tableLayoutPanel1.Controls.Add(bAmb, 5, 0);

            int row = 1;
            foreach (var item in resList)
            {
                Button bName = new Button() { Text = item.Key.Alias[1], Width = 200 };
                bName.Click += (sender, args) => {
                    
                    ActiveTopic = item.Key;
                    cloud.Enabled = false;
                    cloudWorker = new BackgroundWorker();
                    cloudWorker.DoWork += (e, a) =>
                    {
                        try
                        {
                            List<Gma.CodeCloud.Controls.TextAnalyses.Processing.IWord> iwords = new List<Gma.CodeCloud.Controls.TextAnalyses.Processing.IWord>();

                            int cantCloudWords = 0;
                            foreach (var i in item.Value.relevantList)
                            {
                                if (cantCloudWords > 25) break;

                                // Si la palabra es el topic de la nube, la saltea.
                                bool cont = false;
                                foreach (String al in item.Key.Alias)
                                    if (al.Contains(i.Key))
                                        cont = true;
                                if (cont) continue;

                                AnalysisResults intersection = core.GetTopicTermIntersectionAnalysis(item.Key, i.Key);
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

                        pictureBox1.Image = Image.FromStream(core.GetTopicImage(item.Key));
                    };
                    cloudWorker.RunWorkerCompleted += (e, a) => {
                        
                        cloud.Update();
                        cloud.Enabled = true;
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

                if (chart1.Series.FindByName(item.Key.Id) == null)
                {
                    chart1.Series.Add(item.Key.Id);
                    chart1.Series[item.Key.Id].ChartType =
                         System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
                }
                chart1.Series[item.Key.Id].Points.AddXY(graphTime, item.Value.PosVal);
            }

            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.Update();

            graphTime++;
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

       
    }

}
