using System.Collections.Generic;
using System.ServiceModel;
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
