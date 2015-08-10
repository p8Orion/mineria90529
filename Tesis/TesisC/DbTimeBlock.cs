using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TesisC
{
    public class DbTimeBlock
    {
        public AnalysisResults GlobalAR { get; private set; }
        public Dictionary<DbTopic, AnalysisResults> TopicAR { get; private set; }
        public DateTime Start { get; private set; }
        public TimeSpan Length { get; private set; }
        public bool Used { get; set; } // Usado como parte de un bloque más grande

        // Construye un bloque a partir de los últimos 5 minutos
        public DbTimeBlock(DateTime start, TimeSpan length, AnalysisResults globalAR, Dictionary<DbTopic, AnalysisResults> topicAR)
        {
            this.Start = start;
            this.Length = length;
            this.GlobalAR = globalAR;
            this.TopicAR = topicAR;
            this.Used = false;  
        }
    }
}
