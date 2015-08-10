using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TesisC
{
    public class AnalysisResults
    {
        public int PosVal { get; set; }
        public int NegVal { get; set; }
        public float Ambiguity { get; set; }
        public int Popularity { get; set; }
        //public Dictionary<String, double> RelevantTerms { get; private set; }
        public List<KeyValuePair<String, double>> relevantList { get; private set; }
        public Dictionary<String, double> RelevantTerms { get; private set; }

        public AnalysisResults()
        {
            PosVal = 0; NegVal = 0; Ambiguity = 0; Popularity = 0; relevantList = new List<KeyValuePair<string, double>>();
            RelevantTerms = new Dictionary<string, double>();
        }

        public AnalysisResults(int posVal, int negVal, float ambiguity, int popularity, Dictionary<String, double> relevantTerms)
        {
            this.PosVal = posVal;
            this.NegVal = negVal;
            this.Ambiguity = ambiguity;
            this.Popularity = popularity;
            this.RelevantTerms = relevantTerms;

            DictionaryToList();
        }

        public void DictionaryToList()
        {
            relevantList = RelevantTerms.ToList();

            relevantList.Sort((firstPair, nextPair) =>
            {
                return -firstPair.Value.CompareTo(nextPair.Value);
            });
        }

        public void Display()
        {
            Console.Out.WriteLine(PosVal + "/" + NegVal + ", amb: " + Ambiguity + ", pop: " + Popularity + "   ");

            int show = 0;
            foreach (var item in relevantList)
            {
                if (show > 5) break;
                Console.Out.Write("{0:0.##} " + item.Key+", ", item.Value);
                show++;
            }
        }
    }
}
