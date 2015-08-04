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
        public int Weight { get; set; } // Cantidad de Favs
        public int PosValue { get; set; }
        public int NegValue { get; set; }
        public DateTime Publish { get; set; }
        public List<String> About { get; private set; } // Ids de DbTopic
        public List<String> Terms { get; private set; } // Palabras

        public DbTweet(long id, String text, String author, DateTime publish, int weight)
        {
            this.Id = id;
            this.Text = text;
            this.Author = author;
            this.Publish = publish;
            this.Weight = weight + 1; // Tiene que contarse a sí mismo
            this.About = new List<String>();
            this.Terms = new List<String>();
        }
    }
}
