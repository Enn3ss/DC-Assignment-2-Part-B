using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public static class JobLock
    {
        public static bool isLocked;

        static JobLock()
        {
            isLocked = false;
        }
    }
}
