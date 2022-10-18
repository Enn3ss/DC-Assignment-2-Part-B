using System.Collections.Generic;
using System.ServiceModel;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json;
using RestSharp;
using WebServer.Models;

namespace ClientDesktopApp
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = true)]
    internal class JobServer : JobServerInterface
    {
        readonly string localHost = "http://localhost:52998/";

        public List<Job> GetJobs() // Gets the list of jobs in the database
        {
            RestClient client = new RestClient(localHost);
            RestRequest request = new RestRequest("api/Jobs", Method.Get);
            RestResponse response = client.Execute(request);

            return JsonConvert.DeserializeObject<List<Job>>(response.Content);
        }

        public string CompleteJob(string pythonCode) // Runs the python code to do the job
        {
            ScriptEngine engine = Python.CreateEngine();
            ScriptScope scope = engine.CreateScope();
            engine.Execute(pythonCode, scope);

            dynamic testFunction = scope.GetVariable("test");
            var result = testFunction();

            return result.ToString();
        }

        public void UpdateJob(Job completedJob, Client updatedClient)
        {
            // Updating client in the database with new 'JobsFinished'
            RestClient client = new RestClient(localHost);
            RestRequest putRequest = new RestRequest("api/Clients/{id}", Method.Put);
            putRequest.AddUrlSegment("id", updatedClient.Id);
            putRequest.AddJsonBody(updatedClient);
            RestResponse putResponse = client.Execute(putRequest);

            // Deleting the job from the database
            RestRequest deleteRequest = new RestRequest("api/Jobs/{id}", Method.Delete);
            deleteRequest.AddUrlSegment("id", completedJob.Id);
            RestResponse deleteResponse = client.Execute(deleteRequest);
        }
    }
}
