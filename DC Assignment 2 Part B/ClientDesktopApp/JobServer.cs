using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using WebServer.Models;

namespace ClientDesktopApp
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    internal class JobServer : JobServerInterface
    {
        readonly string localHost = "http://localhost:52998/";

        public Job DownloadJob()
        {
            throw new NotImplementedException();
        }

        public List<Job> GetJobs()
        {
            RestClient client = new RestClient(localHost);
            RestRequest request = new RestRequest("api/Jobs", Method.Get);
            RestResponse response = client.Execute(request);

            return JsonConvert.DeserializeObject<List<Job>>(response.Content);
        }

        public void UploadJobSolution(string solution)
        {
            throw new NotImplementedException();
        }
    }
}
