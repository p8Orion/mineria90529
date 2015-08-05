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


namespace TesisC
{
    public class Core
    {
        public const int DOMAIN_ABSTRACTO = 0;
        public const int DOMAIN_NACION = 1;
        public const int DOMAIN_PROVINCIA = 2;

        // Base de datos
        private String dbName = "polArg";
        private IObjectContainer db;

        private IFilteredStream myStream;

        private int cantTweets;
        private int maxCantTweets = 3;

        private TweetAnalyzer TA;

        private Dictionary<String, int> words;

        public Core()
        {
            // Base de datos
            IEmbeddedConfiguration config = Db4oEmbedded.NewConfiguration();
            config.Common.UpdateDepth = 2;  // Para almancenar objetos lista en DB.  
            config.Common.StringEncoding = StringEncodings.Utf8();   // Esto ahorra MUCHO espacio
            db = Db4oEmbedded.OpenFile(config, dbName);

            words = new Dictionary<string, int>();
            InitWords();

            TA = new TweetAnalyzer(db);
        }


        public void StartStream(object sndr, DoWorkEventArgs e)
        {
            cantTweets = 0;

            myStream.StartStreamMatchingAnyCondition();
        }

        public System.IO.Stream GetTopicImage(DbTopic t)
        {
            return Tweetinvi.User.GetProfileImageStream(Tweetinvi.User.GetUserFromScreenName(t.Alias[1]), Tweetinvi.Core.Enum.ImageSize.bigger);
        }

        public void InitListen() 
        {
            cantTweets = 0;

            Console.Out.WriteLine("Tweets test");
            TwitterCredentials.SetCredentials("208552577-ugHwpVaQGNlPHOq3l5W9jKdHR31NcfQX9uBHrLIg", "X6fNijMCqWwPha5jBCJeYcXIeZN42UFautsPz9jlco8S9", "34w3Tz8VE4AUGFrDsDlSsNYgv", "qtvOvqJ0VH4Y2EQlXwHaWFRUyoDhWQlLWtUpvA1OOcuVj7QIsw");

            myStream = Tweetinvi.Stream.CreateFilteredStream();

            DbTopic cfk = new DbTopic(dbName, "CFK", new List<String>() { "cristina kirchner", "@cfkargentina", "cfk" }, null, DOMAIN_NACION);
            ListenTo(cfk);

            // A presidente / vice
            DbTopic sci = new DbTopic(dbName, "Sci", new List<String>() { "scioli", "@danielscioli", "daniel scioli" }, null, DOMAIN_NACION);
            ListenTo(sci);
            DbTopic mac = new DbTopic(dbName, "Mac", new List<String>() { "macri", "@mauriciomacri", "mauricio macri" }, new List<String>() { "jorge" }, DOMAIN_NACION);
            ListenTo(mac);
            DbTopic snz = new DbTopic(dbName, "Sanz", new List<String>() { "sanz", "@sanzernesto", "ernesto sanz" }, new List<String>() { "alejandro" }, DOMAIN_NACION);
            ListenTo(snz);
            DbTopic car = new DbTopic(dbName, "Carr", new List<String>() { "carrió", "@elisacarrio", "carrio", "lilita", "elisa carrio", "elisa carrió" }, null, DOMAIN_NACION);
            ListenTo(car);
            DbTopic mas = new DbTopic(dbName, "Mas", new List<String>() { "massa", "@sergiomassa", "sergio massa" }, null, DOMAIN_NACION);
            ListenTo(mas);
            DbTopic dls = new DbTopic(dbName, "DlS", new List<String>() { "de la sota", "@delasotaok", "manuel de la sota" }, null, DOMAIN_NACION);
            ListenTo(dls);
            DbTopic stol = new DbTopic(dbName, "Stol", new List<String>() { "stolbizer", "@stolbizer", "margarita stolbizer" }, null, DOMAIN_NACION);
            ListenTo(stol);
            DbTopic alt = new DbTopic(dbName, "Alt", new List<String>() { "altamira", "@altamirajorge", "jorge altamira" }, null, DOMAIN_NACION);
            ListenTo(alt);
            DbTopic dca = new DbTopic(dbName, "dCñ", new List<String>() { "del caño", "@nicolasdelcano", "nicolás del caño", "nicolas del caño" }, null, DOMAIN_NACION);
            ListenTo(dca);


            // A gobernador de Buenos Aires / vice
            DbTopic afz = new DbTopic(dbName, "AFz", new List<String>() { "anibal", "@fernandezanibal", "aníbal", "aníbal fernández", "anibal fernandez"}, null, DOMAIN_PROVINCIA);
            ListenTo(afz);
            DbTopic jDz = new DbTopic(dbName, "Mas", new List<String>() { "julian dominguez", "@dominguezjul", "julián domínguez" }, null, DOMAIN_PROVINCIA);
            ListenTo(jDz);
            DbTopic vid = new DbTopic(dbName, "Vid", new List<String>() { "maria eugenia vidal", "@mariuvidal", "maría eugenia vidal" }, null, DOMAIN_PROVINCIA);
            ListenTo(vid);
            DbTopic chi = new DbTopic(dbName, "Chi", new List<String>() { "christian castillo", "@chipicastillo" }, null, DOMAIN_PROVINCIA);
            ListenTo(chi);
            DbTopic pit = new DbTopic(dbName, "Pit", new List<String>() { "pitrola", "@nestorpitrola", "nestor pitrola", }, null, DOMAIN_PROVINCIA);
            ListenTo(pit);
            DbTopic lin = new DbTopic(dbName, "Lin", new List<String>() { "jaime linares", "@linaresjaime" }, null, DOMAIN_PROVINCIA);
            ListenTo(lin);
            DbTopic sol = new DbTopic(dbName, "Sol", new List<String>() { "felipe solá", "@felipe_sola", "felipe sola", }, null, DOMAIN_PROVINCIA);
            ListenTo(sol);
            // Agregar apodos, que presupongan neg?

            // GEOLOC -- Este enfoque no sirve porque filtra los no-geoloc
            /*
            Action<ITweet> act2 = (a) =>
            {
                if (a.Coordinates != null) Console.Out.WriteLine(a.Text + "\n\n");
                else Console.Out.WriteLine(",");
                if (a.Coordinates != null) Console.Out.WriteLine("@ " + a.Coordinates.Latitude + ", " + a.Coordinates.Longitude);
                //if (a.Place != null) Console.Out.WriteLine("En place: " + a.Place);
                if (a.Coordinates != null) Console.Out.WriteLine("*****\n\n");
            };
            
            myStream.AddLocation(new Coordinates(-50.524808, -55.879718), new Coordinates(-71.073531, -21.212183), act2);*/


            //myStream.MatchingTweetReceived += (s, a) => {  };
            //myStream.StartStreamMatchingAnyCondition();

            myStream.StreamStopped += (sender, args) =>
                {
                    Console.Out.WriteLine("***** Stream stopped!");
                };

            //myStream.StartStreamMatchingAnyCondition();

            //System.Threading.Thread.Sleep(20000);

            //myStream.StopStream();
            //db.Close();
        }


        [STAThread]
        static void Main(string[] args)
        {
            FormMainWindow f = new FormMainWindow();
            f.ShowDialog();
        }

        public DbTopic GetDbTopicFromAlias(String s)
        {
            IEnumerable<DbTopic> dbt = from DbTopic x in db select x;

            foreach (DbTopic t in dbt) 
                if (t.Alias.Contains(s)) 
                    return t;

            return null;
        }

        public DbWord GetDbWordFromName(String s)
        {
            IEnumerable<DbWord> ws = from DbWord x in db select x;

            foreach (DbWord w in ws)
                if (w.Name == s)
                    return w;

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

                foreach (DbTopic tp in topics)
                {
                    IEnumerable<DbTweet> tws2 = from DbTweet tw in db
                                                where tw.About.Contains(tp.Id) 
                                                select tw;
                    Console.Out.WriteLine("\n\nResultados para " + tp.Id + ": ");
                    AnalysisResults AR = TA.AnalyzeTweetSet(tws2);
                    toRet.Add(tp, AR);
                    AR.Display();
                }
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
            IEnumerable<DbWord> res = from DbWord x in db
                                      select x;

            if (res.Count() == 0) // No están cargadas las palabras pos/neg/stop
            {
                Console.Out.WriteLine("***** CARGANDO PALABRAS *****\n\n");

                try
                {
                    using (StreamReader sr = new StreamReader("palabraspositivas.txt"))
                    {
                        while (!sr.EndOfStream)
                        {
                            String line = sr.ReadLine(); 
                            db.Store(new DbWord(line, 1));
                            words.Add(line, 1);
                        }
                    }
                    using (StreamReader sr = new StreamReader("palabrasstop.txt"))
                    {
                        while (!sr.EndOfStream)
                        {
                            String line = sr.ReadLine();
                            db.Store(new DbWord(line, 2));
                            words.Add(line, 2);
                        }
                    }
                    using (StreamReader sr = new StreamReader("palabrasnegativas.txt"))
                    {
                        while (!sr.EndOfStream)
                        {
                            String line = sr.ReadLine();
                            db.Store(new DbWord(line, -1));
                            words.Add(line, -1);
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

                if (arg.Coordinates != null) Console.Out.Write(t.Id + "(GEO) ");
                else Console.Out.Write(t.Id + " ");

                if (arg.Language != Tweetinvi.Core.Enum.Language.Spanish) 
                    return;
                if (arg.Coordinates != null) Console.Out.WriteLine("En: " + arg.Coordinates.Latitude + ", " + arg.Coordinates.Longitude);
                if (arg.Place != null) Console.Out.WriteLine("En: " + arg.Place.FullName);
                if (arg.Coordinates != null) Console.Out.WriteLine("*****\n\n");

                IEnumerable<DbTweet> existente = from DbTweet tw in db
                                                 where tw.Id == arg.Id
                                                 select tw;
                
                // Para modificar un tweet ya almacenado (ej. cuando ya mencionaba a otro tópico) en vez de crear otro distinto.
                DbTweet n;
                if (existente != null && existente.Count() == 0)
                {
                    n = new DbTweet(arg.Id, arg.Text, arg.CreatedBy.UserIdentifier.ScreenName, arg.CreatedAt, arg.FavouriteCount);      
                }
                else
                {
                    n = existente.First();
                }

                Console.Out.Write(n.PosValue + "/" + n.NegValue + ", f:" + n.Weight);

                IEnumerable<DbTopic> res = from DbTopic x in db
                                           where x.Id.Equals(t.Id)
                                           select x; 
                DbTopic dbt;
                
                if (res.Count() == 0) dbt = t;
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
