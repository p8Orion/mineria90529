using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Core.Interfaces;
using Tweetinvi.Core.Interfaces.Streaminvi;
using Tweetinvi.Logic.Model;
using Db4objects.Db4o;
using Db4objects.Db4o.Config;
using System.Drawing;

namespace TesisC
{
    public class DbTopic
    {
        public String DbName { get; set; }
        public String Id { get; set; }
        public List<long> TweetsAbout { get; private set; } // ids
        public List<String> Alias { get; private set;  }
        public List<String> Except { get; private set; }
        public int Domain { get; private set; } // Nacional, provincial, abstracto
        public Image[] Image { get; set; }

        public DbTopic(String dbName, String id, List<String> alias, List<String> except, int domain)
        {
            this.DbName = dbName;
            this.Id = id;
            this.Alias = alias;
            
            if (except == null) 
                this.Except = new List<String>();
            else 
                this.Except = except;

            this.TweetsAbout = new List<long>();
            this.Domain = domain;
            this.Image = new Image[3];
        }

        public DbTopic(String dbName, String id, String alias)
        {
            this.DbName = dbName;
            this.Id = id;
            this.Alias = new List<String>();
            this.Alias.Add(alias);
            this.TweetsAbout = new List<long>();
        }

        // Co-ocurrencia, etc.
    }
}
