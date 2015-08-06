﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Tweetinvi;
using Tweetinvi.Core.Interfaces;
using Tweetinvi.Logic.Model;
using Tweetinvi.Core.Interfaces.Streaminvi;
using Tweetinvi.Core.Parameters;
using Db4objects.Db4o;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.Linq;
using Db4objects.Db4o.Config.Encoding;
using System.Timers;


namespace TesisC
{
    public class Core
    {
        public const int Domain_abstracto = 0;
        public const int Domain_nacion = 1;
        public const int Domain_provincia = 2;

        // Base de datos
        private String dbName = "polArg";
        private IObjectContainer db;

        private IFilteredStream myStream;

        private int cantTweets;
        private int maxCantTweets = 20;

        private TweetAnalyzer TA;

        private System.Timers.Timer blockTimer;

        public Dictionary<String, int> words { get; private set; }

        public Core()
        {
            // Base de datos
            IEmbeddedConfiguration config = Db4oEmbedded.NewConfiguration();
            config.Common.UpdateDepth = 3;  // Para almancenar objetos anidados (listas, etc.) en DB.  
            //config.Common.StringEncoding = StringEncodings.Utf8();   // Esto ahorra MUCHO espacio
            db = Db4oEmbedded.OpenFile(config, dbName);

            words = new Dictionary<string, int>();
            InitWords();

            TA = new TweetAnalyzer(db, this);
            blockTimer = new System.Timers.Timer();
            blockTimer.Interval = 1000 * 60;
            blockTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnBlockTimer);
            blockTimer.Start();
        }


        public void StartStream(object sndr, DoWorkEventArgs e)
        {
            cantTweets = 0;
            myStream.StartStreamMatchingAnyCondition();            
        }

        public Image GetTopicImage(int size, DbTopic t)
        {
            if (size == 0)
            {
                if(t.Image[0] == null)
                    t.Image[0] = Image.FromStream(Tweetinvi.User.GetProfileImageStream(Tweetinvi.User.GetUserFromScreenName(t.Alias[1]), Tweetinvi.Core.Enum.ImageSize.mini));
                return t.Image[0];
            }
            else if (size == 1)
            {
                if (t.Image[1] == null)
                    t.Image[1] = Image.FromStream(Tweetinvi.User.GetProfileImageStream(Tweetinvi.User.GetUserFromScreenName(t.Alias[1]), Tweetinvi.Core.Enum.ImageSize.normal));
                return t.Image[1];
            }
            else
            {
                if (t.Image[2] == null)
                    t.Image[2] = Image.FromStream(Tweetinvi.User.GetProfileImageStream(Tweetinvi.User.GetUserFromScreenName(t.Alias[1]), Tweetinvi.Core.Enum.ImageSize.bigger));
                return t.Image[2];
            }
        }

        public IEnumerable<DbTweet> GetTweetsInTimeInterval(DateTime start, DateTime end, DbTopic about)
        {
            IEnumerable<DbTweet> toRet = null;

            if (about == null)
            {
                toRet = from DbTweet tw in db
                        where (start.CompareTo(tw.Added) <= 0 && tw.Added.CompareTo(end) < 0) 
                        select tw;
            }
            else
            {
                toRet = from DbTweet tw in db
                        where (start.CompareTo(tw.Added) <= 0 && tw.Added.CompareTo(end) < 0 && tw.About.Contains(about.Id))
                        select tw;
            }

            return toRet;
        }

        public IEnumerable<DbTopic> GetTopics()
        {
            return db.Query<DbTopic>();
        }

        // Devuelve sólo los intervalos de cierto ancho, ordenados de más viejo a más nuevo
        public IEnumerable<DbTimeBlock> GetTimeBlocks(TimeSpan intervalSize, int amount)
        {
            IEnumerable<DbTimeBlock> toRet = from DbTimeBlock tb in db
                   where tb.Length.CompareTo(intervalSize) == 0
                   orderby tb.Start descending
                   select tb;

            if (toRet.Count() > amount)
                toRet = toRet.Take(amount);
            
            return toRet.Reverse();
        }

        private void OnBlockTimer(object source, ElapsedEventArgs e)
        {
            BuildTimeBlock();
        }

        // Último minuto
        public DbTimeBlock BuildTimeBlock()
        {
            IEnumerable<DbTweet> globalToProc = GetTweetsInTimeInterval(DateTime.Now.AddMinutes(-1), DateTime.Now, null);
            AnalysisResults globalAR = TA.AnalyzeTweetSet(globalToProc);

            Dictionary<DbTopic, AnalysisResults> allTopicsAR = new Dictionary<DbTopic, AnalysisResults>();
            foreach (DbTopic t in db.Query<DbTopic>())
            {
                IEnumerable<DbTweet> topicToProc = GetTweetsInTimeInterval(DateTime.Now.AddMinutes(-1), DateTime.Now, t);
                AnalysisResults topicAR = TA.AnalyzeTweetSet(topicToProc);
                allTopicsAR.Add(t, topicAR);
            }

            DbTimeBlock toRet = new DbTimeBlock(DateTime.Now.AddMinutes(-1), TimeSpan.FromMinutes(1), globalAR, allTopicsAR);
            db.Store(toRet);

            Console.Out.WriteLine("\n\n[ BUILT TIME BLOCK ]\n");
            return toRet;
        }


        public void PurgeDB()
        {
            IEnumerable<DbTweet> toRemove = GetTweetsInTimeInterval(DateTime.Now.AddDays(-1), DateTime.Now.AddMinutes(-1), null); //test, iría -30 o -1 hora, no?

            Console.Out.WriteLine("[ DATABASE CLEANUP START ]");

            Console.Out.WriteLine("[ DbTopics: " + db.Query<DbTopic>().Count + " ]");
            Console.Out.WriteLine("[ DbTweets: " + db.Query<DbTweet>().Count + " ]");
            Console.Out.WriteLine("[ DbTimeBlocks: " + db.Query<DbTimeBlock>().Count + " ]");

            Console.Out.WriteLine("Deleted: ");
            foreach (DbTweet tw in toRemove)
            {
                db.Delete(tw);
                Console.Out.Write(tw.Added + " | ");
            }

            Console.Out.WriteLine("[ Ahora DbTweets: " + db.Query<DbTweet>().Count + " ]");
            Console.Out.WriteLine("[ DATABASE CLEANUP END ]");
        }


        public void InitListen() 
        {
            cantTweets = 0;

            Console.Out.WriteLine("Tweets test");
            TwitterCredentials.SetCredentials("208552577-ugHwpVaQGNlPHOq3l5W9jKdHR31NcfQX9uBHrLIg", "X6fNijMCqWwPha5jBCJeYcXIeZN42UFautsPz9jlco8S9", "34w3Tz8VE4AUGFrDsDlSsNYgv", "qtvOvqJ0VH4Y2EQlXwHaWFRUyoDhWQlLWtUpvA1OOcuVj7QIsw");

            myStream = Tweetinvi.Stream.CreateFilteredStream();

            DbTopic cfk = new DbTopic(dbName, "CFK", new List<String>() { "cristina kirchner", "@cfkargentina", "cfk" }, null, Domain_nacion);
            ListenTo(cfk);

            // A presidente / vice
            DbTopic sci = new DbTopic(dbName, "Sci", new List<String>() { "scioli", "@danielscioli", "daniel scioli" }, null, Domain_nacion);
            ListenTo(sci);
            DbTopic mac = new DbTopic(dbName, "Mac", new List<String>() { "macri", "@mauriciomacri", "mauricio macri" }, new List<String>() { "jorge" }, Domain_nacion);
            ListenTo(mac);
            DbTopic snz = new DbTopic(dbName, "Sanz", new List<String>() { "sanz", "@sanzernesto", "ernesto sanz" }, new List<String>() { "alejandro" }, Domain_nacion);
            ListenTo(snz);
            DbTopic car = new DbTopic(dbName, "Carr", new List<String>() { "carrió", "@elisacarrio", "carrio", "lilita", "elisa carrio", "elisa carrió" }, null, Domain_nacion);
            ListenTo(car);
            DbTopic mas = new DbTopic(dbName, "Mas", new List<String>() { "massa", "@sergiomassa", "sergio massa" }, null, Domain_nacion);
            ListenTo(mas);
            DbTopic dls = new DbTopic(dbName, "DlS", new List<String>() { "de la sota", "@delasotaok", "manuel de la sota" }, null, Domain_nacion);
            ListenTo(dls);
            DbTopic stol = new DbTopic(dbName, "Stol", new List<String>() { "stolbizer", "@stolbizer", "margarita stolbizer" }, null, Domain_nacion);
            ListenTo(stol);
            DbTopic alt = new DbTopic(dbName, "Alt", new List<String>() { "altamira", "@altamirajorge", "jorge altamira" }, null, Domain_nacion);
            ListenTo(alt);
            DbTopic dca = new DbTopic(dbName, "dCñ", new List<String>() { "del caño", "@nicolasdelcano", "nicolás del caño", "nicolas del caño" }, null, Domain_nacion);
            ListenTo(dca);


            // A gobernador de Buenos Aires / vice
            DbTopic afz = new DbTopic(dbName, "AFz", new List<String>() { "anibal", "@fernandezanibal", "aníbal", "aníbal fernández", "anibal fernandez" }, null, Domain_provincia);
            ListenTo(afz);
            DbTopic jDz = new DbTopic(dbName, "Mas", new List<String>() { "julian dominguez", "@dominguezjul", "julián domínguez" }, null, Domain_provincia);
            ListenTo(jDz);
            DbTopic vid = new DbTopic(dbName, "Vid", new List<String>() { "maria eugenia vidal", "@mariuvidal", "maría eugenia vidal" }, null, Domain_provincia);
            ListenTo(vid);
            DbTopic chi = new DbTopic(dbName, "Chi", new List<String>() { "christian castillo", "@chipicastillo" }, null, Domain_provincia);
            ListenTo(chi);
            DbTopic pit = new DbTopic(dbName, "Pit", new List<String>() { "pitrola", "@nestorpitrola", "nestor pitrola", }, null, Domain_provincia);
            ListenTo(pit);
            DbTopic lin = new DbTopic(dbName, "Lin", new List<String>() { "jaime linares", "@linaresjaime" }, null, Domain_provincia);
            ListenTo(lin);
            DbTopic sol = new DbTopic(dbName, "Sol", new List<String>() { "felipe solá", "@felipe_sola", "felipe sola", }, null, Domain_provincia);
            ListenTo(sol);
            // Agregar apodos, que presupongan neg?


            myStream.StreamStopped += (sender, args) =>
                {
                    Console.Out.Write("(Stream stopped) ");
                };
        }


        [STAThread]
        static void Main(string[] args)
        {
            FormMainWindow f = new FormMainWindow();
            f.ShowDialog();
        }


        public IEnumerable<DbTweet> GetGeolocatedTweets(int amount)
        {
            IEnumerable<DbTweet> toRet = (from DbTweet t in db
                                         where t.Coord != null //|| t.Place != null
                                         orderby t.Publish descending
                                         select t).Take(10);

            return toRet;            
        }

        public DbTopic GetDbTopicFromAlias(String s)
        {
            IEnumerable<DbTopic> dbt = from DbTopic x in db select x;

            foreach (DbTopic t in dbt) 
                if (t.Alias.Contains(s)) 
                    return t;

            return null;
        }

        public DbTopic GetDbTopicFromId(String s)
        {
            IEnumerable<DbTopic> dbt = from DbTopic x in db select x;

            foreach (DbTopic t in dbt)
                if (t.Id == s)
                    return t;

            return null;
        }

        public IEnumerable<DbTweet> GetTopicTermIntersectionTweets(DbTopic t, String term)
        {
            try
            {
                IEnumerable<DbTweet> tws = from DbTweet tw in db
                                           where tw.About.Contains(t.Id) && tw.Terms.Contains(term)
                                           select tw;
                return tws;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("GetTopicTermIntersection: " + e.Message);
            }

            return null;
        }

        public AnalysisResults GetTopicTermIntersectionAnalysis(DbTopic t, String term)
        {
            IEnumerable<DbTweet> tws = GetTopicTermIntersectionTweets(t, term);

            if (tws != null)
                return TA.AnalyzeTweetSetLite(tws);
            else
                return null;
        }

        public Dictionary<DbTopic, AnalysisResults> GetTopicsData()
        {
            Dictionary<DbTopic, AnalysisResults> toRet = new Dictionary<DbTopic, AnalysisResults>();

            try
            {
                IEnumerable<DbTopic> topics = from DbTopic x in db
                                              select x;

                Console.Out.WriteLine("***");
                foreach (DbTopic tp in topics)
                {
                    IEnumerable<DbTweet> tws2 = from DbTweet tw in db
                                                where tw.About.Contains(tp.Id) 
                                                select tw;
                    Console.Out.Write("\n\nResultados para " + tp.Id + ": ");
                    AnalysisResults AR = TA.AnalyzeTweetSet(tws2);
                    toRet.Add(tp, AR);
                    AR.Display();
                }
                Console.Out.WriteLine("\n***");
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("GetTopicsData: Error.");
                Console.Out.WriteLine(e.Message);
            }

            return toRet;
        }

        public void InitWords()
        {
            if (words.Count == 0) // No están cargadas las palabras pos/neg/stop
            {               
                Console.Out.WriteLine("***** CARGANDO PALABRAS *****\n\n");

                try
                {
                    using (StreamReader sr = new StreamReader("palabraspositivas.txt"))
                    {
                        while (!sr.EndOfStream)
                        {
                            String line = sr.ReadLine(); 
                            if(!words.ContainsKey(line)) words.Add(line, 1);
                        }
                    }
                    using (StreamReader sr = new StreamReader("palabrasstop.txt"))
                    {
                        while (!sr.EndOfStream)
                        {
                            String line = sr.ReadLine();
                            if (!words.ContainsKey(line)) words.Add(line, 2);
                        }
                    }
                    using (StreamReader sr = new StreamReader("palabrasnegativas.txt"))
                    {
                        while (!sr.EndOfStream)
                        {
                            String line = sr.ReadLine();
                            if (!words.ContainsKey(line)) words.Add(line, -1);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("No se pudo leer el archivo de palabras: ");
                    Console.WriteLine(e.Message);
                }
            }
        }


        public void ListenTo(DbTopic t)
        {
            Action<ITweet> act = (arg) =>
            {
                if (cantTweets >= maxCantTweets)
                {
                    myStream.StopStream();

                    return;
                }

                else Console.Out.Write(t.Id + " ");

                if (arg.Language != Tweetinvi.Core.Enum.Language.Spanish)
                {
                    //Console.Out.WriteLine("!!! No español: " + arg.Text);
                    return;
                }

                IEnumerable<DbTweet> existente = from DbTweet tw in db
                                                 where tw.Id == arg.Id
                                                 select tw;
                
                // Para modificar un tweet ya almacenado (ej. cuando ya mencionaba a otro tópico) en vez de crear otro distinto.
                DbTweet n;
                if (existente != null && existente.Count() == 0)
                {
                    n = new DbTweet(arg.Id, arg.Text, arg.CreatedBy.UserIdentifier.ScreenName, arg.CreatedAt, DateTime.Now, arg.RetweetCount);      
                }
                else
                {
                    n = existente.First();
                }

                if (arg.Coordinates != null)
                {
                    Console.Out.WriteLine("> En: " + arg.Coordinates.Latitude + ", " + arg.Coordinates.Longitude);
                    n.Coord = new Tuple<float, float>((float)arg.Coordinates.Latitude, (float)arg.Coordinates.Longitude);
                }
                else if (arg.Place != null)
                {
                    Console.Out.WriteLine("> En: " + arg.Place.FullName);
                    n.Coord = new Tuple<float, float>((float)arg.Place.BoundingBox.Coordinates[0].Latitude, (float)arg.Place.BoundingBox.Coordinates[0].Longitude);
                    Console.Out.WriteLine("> O sea: " + n.Coord.Item1 +", "+n.Coord.Item2);
                }

                Console.Out.Write(n.PosValue + "/" + n.NegValue + " rt:" + n.RT+" ");


                // Evito topics duplicados si ya están en la base de datos.
                IEnumerable<DbTopic> res = from DbTopic x in db
                                           where x.Id == t.Id
                                           select x; 
                DbTopic dbt;                
                if (res==null || res.Count() == 0) dbt = t;
                else dbt = (DbTopic)res.First();

                // Saltear tweet en caso de que mencione una palabra prohibida para un tópico dado (e.g. "alejandro" en "sanz")
                bool ignore = false;
                foreach (String excep in dbt.Except)
                    if (arg.Text.IndexOf(excep, 0, StringComparison.CurrentCultureIgnoreCase) != -1)
                        ignore = true;

                if (ignore)
                {
                    Console.Out.WriteLine("\n\nIgnorando: " + arg.Text + "\n\n");
                }
                else
                {
                    if(!dbt.TweetsAbout.Contains(n.Id))
                        dbt.TweetsAbout.Add(n.Id);

                    n.About.Add(t.Id);

                    db.Store(n);
                    db.Store(dbt);

                    if (existente.Count() > 0)
                        TA.ProcessTweet(n);

                    cantTweets++;
                }           
            };

            foreach (String a in t.Alias)
            {
                myStream.AddTrack(a, act);
            }
        }


        public void Dispose()
        {
            db.Close();
        }
    }
}
