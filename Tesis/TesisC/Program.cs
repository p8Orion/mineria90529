using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Core.Interfaces;
using Tweetinvi.Logic.Model;
using Tweetinvi.Core.Interfaces.Streaminvi;
using Db4objects.Db4o;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.Linq;


namespace TesisC
{
    class Program
    {
        // Db stuff
        private String dbName = "polArg";
        private IObjectContainer db;

        private IFilteredStream myStream;

        private int cantTweets;
        private int maxCantTweets = 10;

        public Program()
        {
            cantTweets = 0;

            // Db stuff
            IEmbeddedConfiguration config = Db4oEmbedded.NewConfiguration();
            config.Common.UpdateDepth = 2;
            db = Db4oEmbedded.OpenFile(config, dbName);
            // Para almancenar listas en DB.            
            


            Console.Out.WriteLine("Tweets test");
            TwitterCredentials.SetCredentials("208552577-ugHwpVaQGNlPHOq3l5W9jKdHR31NcfQX9uBHrLIg", "X6fNijMCqWwPha5jBCJeYcXIeZN42UFautsPz9jlco8S9", "34w3Tz8VE4AUGFrDsDlSsNYgv", "qtvOvqJ0VH4Y2EQlXwHaWFRUyoDhWQlLWtUpvA1OOcuVj7QIsw");

            myStream = Stream.CreateFilteredStream();

            DbTopic mac = new DbTopic(dbName, "Mac", new List<String>() { "macri", "@mauriciomacri" });
            ListenTo(mac);
            DbTopic cfk = new DbTopic(dbName, "CFK", new List<String>() { "cristina", "@cfkargentina", "cfk" });
            ListenTo(cfk);
            DbTopic mas = new DbTopic(dbName, "Mas", new List<String>() { "massa", "@SergioMassa" });
            ListenTo(mas);
            DbTopic sci = new DbTopic(dbName, "Sci", new List<String>() { "scioli", "@DanielScioli" });
            ListenTo(sci);
            DbTopic rzo = new DbTopic(dbName, "Rzo", new List<String>() { "randazzo", "@RandazzoFa" });
            ListenTo(rzo);
            DbTopic afz = new DbTopic(dbName, "AFz", new List<String>() { "anibal fernandez", "aníbal fernández", "@FernandezAnibal" });
            ListenTo(afz);
            DbTopic bin = new DbTopic(dbName, "Bin", new List<String>() { "binner", "@HermesBinner" });
            ListenTo(bin);
            DbTopic cob = new DbTopic(dbName, "Cob", new List<String>() { "cobos", "@juliocobos" });
            ListenTo(cob);


            var nw = new Coordinates(-50.524808, -55.879718);
            var se = new Coordinates(-71.073531, -21.212183);
            Location ar = new Location(nw, se);

            Action<ITweet> act2 = (a) =>
            {
                if (a.Coordinates != null) Console.Out.WriteLine(a.Text + "\n\n");
                else Console.Out.WriteLine(",");
                if (a.Coordinates != null) Console.Out.WriteLine("@ " + a.Coordinates.Latitude + ", " + a.Coordinates.Longitude);
                //if (a.Place != null) Console.Out.WriteLine("En place: " + a.Place);
                if (a.Coordinates != null) Console.Out.WriteLine("*****\n\n");
            };
            //myStream.AddLocation(ar, act2);

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
                //if(a.Coordinates != null) Console.Out.WriteLine("En: " + a.Coordinates.Latitude + ", " + a.Coordinates.Longitude);
                //if (a.Place != null) Console.Out.WriteLine("En place: " + a.Place);
                //if (a.Coordinates != null) Console.Out.WriteLine("*****\n\n");

                DbTweet n = new DbTweet(arg.Id.ToString(), arg.Text, arg.Creator.UserIdentifier.ScreenName, arg.CreatedAt, arg.FavouriteCount);

                IEnumerable<DbTopic> res = from DbTopic x in db
                                           where x.Id.Equals(t.Id)
                                           select x; 
                DbTopic dbt;
                           
                if (res.Count() == 0) dbt = t;
                else dbt = (DbTopic)res.First();
                
                dbt.TweetsAbout.Add(n.Id);

                n.About.Add(t.Id);

                db.Store(n);
                db.Store(dbt);

                cantTweets++;
            };

            foreach (String a in t.Alias)
            {
                myStream.AddTrack(a, act);
            }
        }

    }
}
