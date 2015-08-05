using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Db4objects.Db4o;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.Linq;

namespace TesisC
{
    public class TweetAnalyzer
    {
        private IObjectContainer db;
        private Core Core;
        private Dictionary<String, int> TermFreq; // Frecuencia de los términos en todo el corpus de tweets. Se carga desde la base de datos al inicializar y se va actualizando al aparecer tweets nuevos.
        private int cantTweets;

        public TweetAnalyzer(IObjectContainer database, Core core)
        {
            this.Core = core;
            this.db = database;
            cantTweets = 0;
            TermFreq = new Dictionary<String, int>();
            Initialize();
        }

        // Carga la información sobre los términos que ya está en la base de datos
        public void Initialize()
        {
            IEnumerable<DbTweet> tws = from DbTweet tw in db
                                       select tw;

            cantTweets = tws.Count();

            foreach (DbTweet tw in tws) {
                foreach (String word in tw.Terms) {
                    if (TermFreq.ContainsKey(word.ToLower())) TermFreq[word.ToLower()]++;
                    else TermFreq[word.ToLower()] = 1;
                }
            }

            Console.Out.WriteLine("\nTermFreq:\n");
            foreach(var item in TermFreq) {
                Console.Out.Write("Freq: " + item.Key + ": " + item.Value + ", ");
            }
        }


        public AnalysisResults AnalyzeTweetSetLite(IEnumerable<DbTweet> tws)
        {
            int posVal = 0;
            int negVal = 0;
            int popularity = 0;

            foreach (DbTweet tw in tws)
            {
                posVal += tw.PosValue * tw.Weight;
                negVal += tw.NegValue * tw.Weight;
                popularity += tw.Weight;
            }

            return new AnalysisResults(posVal, negVal, 0, popularity, new Dictionary<String,double>());
        }

        public AnalysisResults AnalyzeTweetSet(IEnumerable<DbTweet> tws)
        {
            Dictionary<String, int> termAppearances = new Dictionary<String, int>();
            Dictionary<String, double> relevantTerms = new Dictionary<String, double>();
            int posVal = 0;
            int negVal = 0;
            int popularity = 0;
            float ambiguity = 0;
            int ambiguityOver = 0; // Para ignorar los tweets sin positivos ni negativos en este cálculo.
            int ct = tws.Count();

            try
            {
                foreach (DbTweet tw in tws) {
                    posVal += tw.PosValue * tw.Weight;
                    negVal += tw.NegValue * tw.Weight;
                    if (tw.PosValue + tw.NegValue > 0) {
                        ambiguity += 1 - (Math.Abs(tw.PosValue - tw.NegValue) / (tw.PosValue + tw.NegValue)); // |x-y|/(x+y)
                        ambiguityOver++;
                    }
                    popularity += tw.Weight;

                    foreach(String term in tw.Terms) {
                        if (termAppearances.ContainsKey(term)) termAppearances[term]++;
                        else termAppearances[term] = 1;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
            }

            if(ambiguityOver>0)
                ambiguity = ambiguity/ambiguityOver;
         
            foreach (var item in termAppearances)
            {
                try
                {
                    if(!TermFreq.ContainsKey(item.Key)) Console.Out.WriteLine("No se pudo calcular idf para " + item.Key + ". Esto debiera ser imposible.");
                    double idf = Math.Log(cantTweets / TermFreq[item.Key]);
                    relevantTerms.Add(item.Key, item.Value * idf);
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                }
            }          

            return new AnalysisResults(posVal, negVal, ambiguity, popularity, relevantTerms);
        }


        public void ProcessTweet(DbTweet tw)
        {
            try
            {
                String[] words = tw.Text.Split(new char[] { ' ', '.', ',', '?', '!', '¿', '¡', ';', ':', '-', '\n', '(', ')', '"', '\'', '[', ']', '+', '|', '“', '”', '…'}, StringSplitOptions.RemoveEmptyEntries);

                foreach (String w in words)
                {
                    if (w.Contains('/')) continue; // Ignoro urls
                    if (w.Length == 1) continue;
                    if (w.Contains('@')) // Sólo guardo @alias de los topics
                    {
                        bool cont = true;
                        IEnumerable<String> topicUsers = from DbTopic t in db
                                                 select t.Alias[1];
                        
                        foreach(String u in topicUsers)
                            if(w.ToLower() == u.ToLower())
                                cont = false;

                        if (cont) continue;
                    }            

                    /*
                    IEnumerable<DbWord> res = from DbWord x in db
                                           where x.Name.Equals(w.ToLower())
                                           select x;*/

                    int wordValue = 0;
                    if (Core.words.ContainsKey(w.ToLower()))
                       wordValue = Core.words[w.ToLower()]; // En lugar de res.First, etc.

                    bool stopword = false;
                    if (wordValue == 2) // Caso stopword
                    {
                        stopword = true;
                        //Console.Out.WriteLine("Stopword: " + w);
                    }
                    else if (wordValue == 1) tw.PosValue++; // Palabra positiva
                    else if (wordValue == -1) tw.NegValue++; // Palabra negativa
                    else // Caso en que la palabra es el alias de un topic.
                    {
                        IEnumerable<DbTopic> tps = from DbTopic t in db
                                                    select t;
                        
                        DbTopic isTopic = null;
                        foreach (DbTopic tp in tps)
                        {
                            foreach (String a in tp.Alias)
                                if (w.ToLower().Contains(a.ToLower()))
                                {
                                    isTopic = tp;
                                    break;
                                }
                        }
                        if (isTopic != null && !isTopic.Alias.Contains(w.ToLower()))
                        {
                            isTopic.Alias.Add(w.ToLower());
                            Console.Out.WriteLine("Agregado " + w.ToLower() + " como topic a " + isTopic.Id);
                        }
                    }

                    if (!stopword && w != "")
                    {
                        // Se agrega el término
                        if (!tw.Terms.Contains(w.ToLower()))
                            tw.Terms.Add(w.ToLower());

                        if (TermFreq.ContainsKey(w.ToLower())) TermFreq[w.ToLower()]++;
                        else TermFreq[w.ToLower()] = 1;
                    }                                  
                }

                db.Store(tw);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("ProcessTweet: Error analizando tweet.");
                Console.Out.WriteLine(e.Message + "\n" + e.StackTrace);
                
            }

            cantTweets++;
        }

    }
}
