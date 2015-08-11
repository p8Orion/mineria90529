using System;
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
using System.Net;


namespace TesisC
{
    public class Core
    {
        public const int Domain_abstracto = 0;
        public const int Domain_nacion = 1;
        public const int Domain_provincia = 2;

        public TimeSpan TS_quick = TimeSpan.FromSeconds(15);
        //public TimeSpan TS_short = TimeSpan.FromMinutes(5);
        //public TimeSpan TS_medium = TimeSpan.FromHours(1); 
        //public TimeSpan TS_long = TimeSpan.FromDays(1);
        public TimeSpan TS_short = TimeSpan.FromSeconds(5);
        public TimeSpan TS_medium = TimeSpan.FromSeconds(25);
        public TimeSpan TS_long = TimeSpan.FromSeconds(125);
        private int TimeBlockConsolidationThreshold = 5;

        // Base de datos
        private String dbName = "polArg";
        private IObjectContainer db;

        private IFilteredStream myStream;

        private int cantTweets;
        private int maxCantTweets = 50;

        private TweetAnalyzer TA;

        private System.Timers.Timer longBlockTimer, mediumBlockTimer, shortBlockTimer, quickBlockTimer;

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

            mediumBlockTimer = new System.Timers.Timer();
            mediumBlockTimer.Interval = TS_medium.TotalMilliseconds;
            mediumBlockTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnMediumBlockTimer);
            
            longBlockTimer = new System.Timers.Timer();
            longBlockTimer.Interval = TS_long.TotalMilliseconds;
            longBlockTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnLongBlockTimer);

            shortBlockTimer = new System.Timers.Timer();
            shortBlockTimer.Interval = TS_short.TotalMilliseconds;
            shortBlockTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnShortBlockTimer);

            quickBlockTimer = new System.Timers.Timer();
            quickBlockTimer.Interval = TS_quick.TotalMilliseconds;
            quickBlockTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnQuickBlockTimer);
        }


        public void StartStream(object sndr, DoWorkEventArgs e)
        {
            cantTweets = 0;
            myStream.StartStreamMatchingAnyCondition();

            shortBlockTimer.Start();
            quickBlockTimer.Start();
            mediumBlockTimer.Start();
            longBlockTimer.Start();
        }

        public Image GetTopicImage(int size, DbTopic t)
        {
            Image im = null;
            
            if (size == 0)
            {            
                if(File.Exists(t.Id+"_0"))
                    im = Image.FromFile(t.Id+"_0");
                if (im == null)
                {
                    im = Image.FromStream(Tweetinvi.User.GetProfileImageStream(Tweetinvi.User.GetUserFromScreenName(t.Alias[1]), Tweetinvi.Core.Enum.ImageSize.mini));
                    im.Save(t.Id + "_0");
                }
                return im;
            }
            else if (size == 1)
            {
                if (File.Exists(t.Id + "_1"))
                    im = Image.FromFile(t.Id + "_1");
                if (im == null)
                {
                    im = Image.FromStream(Tweetinvi.User.GetProfileImageStream(Tweetinvi.User.GetUserFromScreenName(t.Alias[1]), Tweetinvi.Core.Enum.ImageSize.normal));
                    im.Save(t.Id + "_1");
                }
                return im;
            }
            else
            {
                if (File.Exists(t.Id + "_2"))
                    im = Image.FromFile(t.Id + "_2");
                if (im == null)
                {
                    im = Image.FromStream(Tweetinvi.User.GetProfileImageStream(Tweetinvi.User.GetUserFromScreenName(t.Alias[1]), Tweetinvi.Core.Enum.ImageSize.bigger));
                    im.Save(t.Id + "_2");
                }
                return im;
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
            /* Versión getLatest...
            List<DbTimeBlock> toRet = new List<DbTimeBlock>();
       
            for (int i = 1; i <= amount; i++)
            {
                TimeSpan tsi = TimeSpan.FromTicks(-intervalSize.Ticks * i);
                IEnumerable<DbTimeBlock> tbs = from DbTimeBlock tb in db
                                               where tb.Length.CompareTo(intervalSize) == 0 &&
                                                  tb.Start.Add(tsi).CompareTo(DateTime.Now.Add(tsi)) < 0 &&
                                                  tb.Start.Add(tsi).Add(intervalSize).CompareTo(DateTime.Now.Add(tsi)) >= 0
                                               select tb;

                toRet.Add(tbs.FirstOrDefault());
            }*/

            TimeSpan tsamount = TimeSpan.FromTicks(intervalSize.Ticks * (amount+1));
            IEnumerable<DbTimeBlock> toRet = from DbTimeBlock tb in db
                   where (tb.Length.CompareTo(intervalSize) == 0) && tb.Start.Add(tsamount).CompareTo(DateTime.Now) > 0 // i.e. empezó hace menos de amount+1 intervalos.
                   orderby tb.Start descending
                   select tb;
            
            if (toRet.Count() > amount)
                toRet = toRet.Take(amount);

            return toRet.Reverse();
        }

        private void OnShortBlockTimer(object source, ElapsedEventArgs e)
        {
            BuildTimeBlockFromTweets(false, this.TS_short);
        }

        private void OnMediumBlockTimer(object source, ElapsedEventArgs e)
        {
            BuildTimeBlockFromBlocks(this.TS_medium);
        }

        private void OnLongBlockTimer(object source, ElapsedEventArgs e)
        {
            BuildTimeBlockFromBlocks(this.TS_long);
        }

        private void OnQuickBlockTimer(object source, ElapsedEventArgs e)
        {
            BuildTimeBlockFromTweets(true, this.TS_quick);
        }

        // Construye bloques de último momento (sin detalle, quick), o pequeños, basados en tweets (i.e. no en otros bloques menores).
        public DbTimeBlock BuildTimeBlockFromTweets(bool quick, TimeSpan length)
        {
            IEnumerable<DbTweet> globalToProc = GetTweetsInTimeInterval(DateTime.Now.Add(-length), DateTime.Now, null);

            AnalysisResults globalAR = null;
            if(quick)
                globalAR = TA.AnalyzeTweetSet(globalToProc, true);
            else
                globalAR = TA.AnalyzeTweetSet(globalToProc, false);

            Dictionary<DbTopic, AnalysisResults> allTopicsAR = new Dictionary<DbTopic, AnalysisResults>();
            foreach (DbTopic t in db.Query<DbTopic>())
            {
                IEnumerable<DbTweet> topicToProc = GetTweetsInTimeInterval(DateTime.Now.Add(-length), DateTime.Now, t);

                AnalysisResults topicAR = null;
                if(quick)
                    topicAR = TA.AnalyzeTweetSet(topicToProc, true);
                else
                    topicAR = TA.AnalyzeTweetSet(topicToProc, false);

                allTopicsAR.Add(t, topicAR);
            }

            DbTimeBlock toRet = new DbTimeBlock(DateTime.Now.Add(-length), length, globalAR, allTopicsAR); 
            db.Store(toRet);

            Console.Out.WriteLine("\n\n[ BUILT TIME BLOCK, quick: "+quick+"+ ]\n");

            return toRet;
        }

        // Construye bloques medianos o grandes, basados en bloques chicos o medianos respectivamente.
        public void BuildTimeBlockFromBlocks(TimeSpan length)
        {
            TimeSpan childBlocksLength = TimeSpan.FromSeconds(0);
            if (length == TS_medium)
                childBlocksLength = TS_short;
            else if (length == TS_long)
                childBlocksLength = TS_medium;

            IEnumerable<DbTimeBlock> ch = from DbTimeBlock t in db
                                         where t.Length == childBlocksLength && t.Used == false
                                         orderby t.Start descending
                                         select t;
            int cant = ch.Count();
            AnalysisResults GlobalAR = new AnalysisResults();
            Dictionary<DbTopic, AnalysisResults> TopicAR = new Dictionary<DbTopic, AnalysisResults>();
            foreach (DbTopic t in db.Query<DbTopic>())
                TopicAR.Add(t, new AnalysisResults());

            if (cant > this.TimeBlockConsolidationThreshold)
            {
                foreach (DbTimeBlock cht in ch)
                {
                    GlobalAR.Popularity += cht.GlobalAR.Popularity;
                    GlobalAR.PosVal += cht.GlobalAR.PosVal;
                    GlobalAR.NegVal += cht.GlobalAR.NegVal;
                    GlobalAR.Ambiguity += cht.GlobalAR.Ambiguity;

                    // Para poder comparar las palabras importantes de un subbloque con otro, ya que cada uno fue calculado con tfidf distintos.
                    double normalizationFactor = 0;
                    foreach(KeyValuePair<string,double> kv in cht.GlobalAR.relevantList)
                        normalizationFactor += kv.Value;

                    foreach(KeyValuePair<string,double> kv in cht.GlobalAR.relevantList)
                        if(GlobalAR.RelevantTerms.ContainsKey(kv.Key))
                            GlobalAR.RelevantTerms[kv.Key] += kv.Value/normalizationFactor;
                        else
                            GlobalAR.RelevantTerms[kv.Key] = kv.Value/normalizationFactor;

                    // ídem a lo anterior, para cada topic
                    foreach (KeyValuePair<DbTopic, AnalysisResults> tar in cht.TopicAR)
                    {
                        TopicAR[tar.Key].Popularity += tar.Value.Popularity;
                        TopicAR[tar.Key].PosVal += tar.Value.PosVal;
                        TopicAR[tar.Key].NegVal += tar.Value.NegVal;
                        TopicAR[tar.Key].Ambiguity += tar.Value.Ambiguity;     
                  
                        normalizationFactor = 0;
                        foreach(KeyValuePair<string,double> kv in cht.TopicAR[tar.Key].relevantList)
                            normalizationFactor += kv.Value;

                        foreach(KeyValuePair<string,double> kv in cht.TopicAR[tar.Key].relevantList)
                            if(TopicAR[tar.Key].RelevantTerms.ContainsKey(kv.Key))
                                TopicAR[tar.Key].RelevantTerms[kv.Key] += kv.Value/normalizationFactor;
                            else
                                TopicAR[tar.Key].RelevantTerms[kv.Key] = kv.Value / normalizationFactor;
                    }

                    cht.Used = true;
                    db.Store(cht);
                }
              
                GlobalAR.Ambiguity /= cant;
                GlobalAR.DictionaryToList();

                foreach (DbTopic t in db.Query<DbTopic>())
                {
                    TopicAR[t].Ambiguity /= cant;
                    TopicAR[t].DictionaryToList();
                }

                DbTimeBlock toAdd = new DbTimeBlock(DateTime.Now.Add(-length), length, GlobalAR, TopicAR);
                db.Store(toAdd);

                Console.Out.WriteLine("\n\n[ BUILT TIME BLOCK FROM SMALL BLOCKS, size: " + length +"+ ]\n");
            }   
        }


        public void PurgeDB()
        {
            IEnumerable<DbTweet> toRemove = GetTweetsInTimeInterval(DateTime.MinValue, DateTime.Now.Add(this.TS_short), null);

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

        public AnalysisResults GetTopicTermIntersectionAnalysis(DbTopic t, String term, bool detailed)
        {
            IEnumerable<DbTweet> tws = GetTopicTermIntersectionTweets(t, term);

            if (tws != null)
                if (detailed) return TA.AnalyzeTweetSet(tws, false);
                else return TA.AnalyzeTweetSet(tws, true);
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
                    Console.Out.WriteLine("Resultados para " + tp.Id + ": ");
                    AnalysisResults AR = TA.AnalyzeTweetSet(tws2, true);
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


        public bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (var stream = client.OpenRead("http://www.google.com"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            db.Close();
        }
    }
}
