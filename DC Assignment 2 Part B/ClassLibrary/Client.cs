using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class Client
    {
        public int Id { get; set; }
        public string IPAddress { get; set; }
        public string Port { get; set; }
        public Nullable<int> JobsFinished { get; set; }
    }
}
