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

namespace TesisC
{
    class DbTopic
    {
        public String DbName { get; set; }
        public String Id { get; set; }
        public List<String> TweetsAbout { get; private set; } // ids
        public List<String> Alias { get; private set;  }

        public DbTopic(String dbName, String id, List<String> alias)
        {
            this.DbName = dbName;
            this.Id = id;
            this.Alias = alias;
            this.TweetsAbout = new List<String>();
        }

        public DbTopic(String dbName, String id, String alias)
        {
            this.DbName = dbName;
            this.Id = id;
            this.Alias = new List<String>();
            this.Alias.Add(alias);
            this.TweetsAbout = new List<String>();
        }

        // Co-ocurrencia, etc.
    }
}
