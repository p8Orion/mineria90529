using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Tweetinvi;
using Tweetinvi.Core.Interfaces;
using Tweetinvi.Logic.Model;
using Tweetinvi.Core.Interfaces.Streaminvi;
using Db4objects.Db4o;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.Linq;
using Tweetinvi.Core.Parameters;



namespace TesisC
{
    class Program
    {
        // Base de datos
        private String dbName = "polArg";
        private IObjectContainer db;

        private IFilteredStream myStream;

        private int cantTweets;
        private int maxCantTweets = 10;

        public Program()
        {
            cantTweets = 0;

            // Base de datos
            IEmbeddedConfiguration config = Db4oEmbedded.NewConfiguration();
            config.Common.UpdateDepth = 2;      // Para almancenar objetos lista en DB.  
            db = Db4oEmbedded.OpenFile(config, dbName);

            InitWords();

            Console.Out.WriteLine("Tweets test");
            TwitterCredentials.SetCredentials("208552577-ugHwpVaQGNlPHOq3l5W9jKdHR31NcfQX9uBHrLIg", "X6fNijMCqWwPha5jBCJeYcXIeZN42UFautsPz9jlco8S9", "34w3Tz8VE4AUGFrDsDlSsNYgv", "qtvOvqJ0VH4Y2EQlXwHaWFRUyoDhWQlLWtUpvA1OOcuVj7QIsw");

            myStream = Tweetinvi.Stream.CreateFilteredStream();

            DbTopic cfk = new DbTopic(dbName, "CFK", new List<String>() { "cristina kirchner", "@cfkargentina", "cfk" }, null);
            ListenTo(cfk);

            // A presidente / vice
            DbTopic sci = new DbTopic(dbName, "Sci", new List<String>() { "scioli", "@DanielScioli" }, null);
            ListenTo(sci);
            DbTopic mac = new DbTopic(dbName, "Mac", new List<String>() { "macri", "@mauriciomacri" }, new List<String>() {"jorge"});
            ListenTo(mac);
            DbTopic snz = new DbTopic(dbName, "Sanz", new List<String>() { "sanz", "@SanzErnesto" }, new List<String>() {"alejandro"});
            ListenTo(snz);
            DbTopic car = new DbTopic(dbName, "Carr", new List<String>() { "carrió", "carrio", "lilita", "@elisacarrio" }, null);
            ListenTo(car);
            DbTopic mas = new DbTopic(dbName, "Mas", new List<String>() { "massa", "@SergioMassa" }, null);
            ListenTo(mas);
            DbTopic dls = new DbTopic(dbName, "DlS", new List<String>() { "de la sota", "@delasotaok" }, null);
            ListenTo(dls);
            DbTopic stol = new DbTopic(dbName, "Stol", new List<String>() { "stolbizer", "@Stolbizer" }, null);
            ListenTo(stol);
            DbTopic alt = new DbTopic(dbName, "Alt", new List<String>() { "altamira", "@altamirajorge" }, null);
            ListenTo(alt);
            DbTopic dca = new DbTopic(dbName, "dCñ", new List<String>() { "del caño", "@NicolasdelCano" }, null);
            ListenTo(dca);


            // A gobernador de Buenos Aires / vice
            DbTopic afz = new DbTopic(dbName, "AFz", new List<String>() { "anibal", "aníbal", "@FernandezAnibal" }, null);
            ListenTo(afz);
            DbTopic jDz = new DbTopic(dbName, "Mas", new List<String>() { "julian dominguez", "julián domínguez", "@DominguezJul" }, null);
            ListenTo(jDz);
            DbTopic vid = new DbTopic(dbName, "Vid", new List<String>() { "maria eugenia vidal", "maría eugenia vidal", "@mariuvidal" }, null);
            ListenTo(vid);
            DbTopic chi = new DbTopic(dbName, "Chi", new List<String>() { "christian castillo", "@chipicastillo" }, null);
            ListenTo(chi);
            DbTopic pit = new DbTopic(dbName, "Pit", new List<String>() { "pitrola", "pitrola", "@nestorpitrola" }, null);
            ListenTo(pit);
            DbTopic lin = new DbTopic(dbName, "Lin", new List<String>() { "jaime linares", "@LinaresJaime" }, null);
            ListenTo(lin);
            DbTopic sol = new DbTopic(dbName, "Sol", new List<String>() { "felipe solá", "felipe sola", "@felipe_sola" }, null);
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
            myStream.StartStreamMatchingAnyCondition();

            //System.Threading.Thread.Sleep(20000);

            //myStream.StopStream();
            //db.Close();
        }


        static void Main(string[] args)
        {
            Program p = new Program();
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
                        }
                    }
                    using (StreamReader sr = new StreamReader("palabrasstop.txt"))
                    {
                        while (!sr.EndOfStream)
                        {
                            String line = sr.ReadLine();
                            db.Store(new DbWord(line, 0));
                        }
                    }
                    using (StreamReader sr = new StreamReader("palabrasnegativas.txt"))
                    {
                        while (!sr.EndOfStream)
                        {
                            String line = sr.ReadLine();
                            db.Store(new DbWord(line, -1));
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



        public void AnalyzeTweet(DbTweet tw)
        {
            String[] words = tw.Text.Split(new char[] { ' ', '.', ',', '?', '!', '¿', '¡', ';', ':', '-', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (String w in words)
            {
                IEnumerable<int> res = from DbWord x in db
                                       where x.Name.Equals(w.ToLower())
                                       select x.Value;

                if (res.Count() == 0) // Potencial palabra útil (i.e. no positiva, negativa, stopword... ni topic?)
                {
                }
                else
                {
                    if (res.First() == 1) tw.PosValue++;
                    if (res.First() == -1) tw.NegValue++;
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
                    db.Close();
                    return;
                }

                if (arg.Coordinates != null) Console.Out.Write(t.Id + "(GEO) ");
                else Console.Out.Write(t.Id + " ");

                if (arg.Language != Tweetinvi.Core.Enum.Language.Spanish) 
                    return;
                if (arg.Coordinates != null) Console.Out.WriteLine("En: " + arg.Coordinates.Latitude + ", " + arg.Coordinates.Longitude);
                if (arg.Place != null) Console.Out.WriteLine("En: " + arg.Place.FullName);
                if (arg.Coordinates != null) Console.Out.WriteLine("*****\n\n");

                DbTweet n = new DbTweet(arg.Id, arg.Text, arg.CreatedBy.UserIdentifier.ScreenName, arg.CreatedAt, arg.FavouriteCount);
                
                AnalyzeTweet(n);
                Console.Out.Write(n.PosValue + "/" + n.NegValue + " ");

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

                    cantTweets++;
                }
            };

            foreach (String a in t.Alias)
            {
                myStream.AddTrack(a, act);
            }
        }

    }
}
