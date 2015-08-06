using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Db4objects.Db4o;
using Db4objects.Db4o.Ext;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.Query;


namespace TesisC
{
    public class DbTweet
    {
        public long Id { get; set; }
        public String Text { get; set; }
        public String Author { get; set; }
        public int RT { get; set; } // Cantidad de retweets
        public int PosValue { get; set; }
        public int NegValue { get; set; }
        public DateTime Publish { get; set; }
        public DateTime Added { get; set; }
        public List<String> About { get; private set; } // Ids de DbTopic
        public List<String> Terms { get; private set; } // Palabras
        public Tuple<float, float> Coord { get; set; } // Coordenadas geoloc
        //public String Place { get; set; } // Place geoloc

        public DbTweet(long id, String text, String author, DateTime publish, DateTime added, int rt)
        {
            this.Id = id;
            this.Text = text;
            this.Author = author;
            this.Publish = publish;
            this.Added = added;
            this.RT = rt;
            this.About = new List<String>();
            this.Terms = new List<String>();
            //this.Place = null;
            this.Coord = null;
        }
    }
}
