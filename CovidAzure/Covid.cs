using System;
using System.Collections.Generic;
using System.Text;

namespace CovidAzure
{
    public class Covid
    {
        public IEnumerable<Data> Data { get; set; }
    }

    public class Data
    {
        public DateTime Date { get; set; }
        
        public int New { get; set; }

        public int? Total { get; set; }
    }
}
