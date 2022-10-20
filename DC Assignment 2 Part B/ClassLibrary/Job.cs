using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class Job
    {
        public int Id { get; set; }
        public Nullable<int> ClientId { get; set; }
        public string PythonCode { get; set; }
    }
}
