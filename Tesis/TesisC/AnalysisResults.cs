using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TesisC
{
    public class AnalysisResults
    {
        public int PosVal { get; private set; }
        public int NegVal { get; private set; }
        public float Ambiguity { get; private set; }
        public int Popularity { get; private set; }
        public Dictionary<String, double> RelevantTerms { get; private set; }

        public AnalysisResults(int posVal, int negVal, float ambiguity, int popularity, Dictionary<String, double> relevantTerms)
        {
            this.PosVal = posVal;
            this.NegVal = negVal;
            this.Ambiguity = ambiguity;
            this.Popularity = popularity;
            this.RelevantTerms = relevantTerms;
        }

        public void Display()
        {
            List<KeyValuePair<String, double>> relevantList = RelevantTerms.ToList();

            relevantList.Sort((firstPair, nextPair) =>
                {
                    return -firstPair.Value.CompareTo(nextPair.Value);
                }
            );

            Console.Out.WriteLine(PosVal + "/" + NegVal + ", amb: " + Ambiguity + ", pop: " + Popularity);
            Console.Out.WriteLine("Relevant terms: ");

            int show = 0;
            foreach (var item in relevantList)
            {
                if (show > 10) break;
                Console.Out.Write("{0:0.##} " + item.Key+", ", item.Value);
                show++;
            }
        }
    }
}
