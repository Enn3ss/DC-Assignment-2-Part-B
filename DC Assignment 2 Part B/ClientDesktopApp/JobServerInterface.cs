using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using WebServer.Models;

namespace ClientDesktopApp
{
    [ServiceContract]
    internal interface JobServerInterface
    {
        [OperationContract]
        List<Job> GetJobs();

        [OperationContract]
        string CompleteJob(string pythonCode);

        [OperationContract]
        void UpdateJob(Job completedJob, Client updatedClient);
    }
}
