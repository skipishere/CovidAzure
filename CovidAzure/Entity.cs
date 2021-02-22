using System;

namespace CovidAzure
{
    internal class Entity
    {
        public string Entity_id { get; set; }

        public DateTime Last_changed { get; set; }

        public string State { get; set; }
    }
}