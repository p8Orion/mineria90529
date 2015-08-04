using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TesisC
{
    public class DbWord
    {
        public int Value {get; set;} // -1 negativa, 0 stopword, 1 positiva
        public String Name { get; set; }

        public DbWord(String name, int value)
        {
            this.Name = name;
            this.Value = value;
        }
    }
}
