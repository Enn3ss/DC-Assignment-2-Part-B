using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using ClassLibrary;

namespace WebsiteGUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly string WEBSERVER_URL = "http://localhost:52998/";
        public List<Client> clientList;

        public IActionResult Index()
        {
            ViewBag.Title = "Home";

            RestClient restClient = new RestClient(WEBSERVER_URL);
            RestRequest restRequest = new RestRequest("api/Clients/", Method.Get);
            RestResponse restResponse = restClient.Get(restRequest);

            clientList = JsonConvert.DeserializeObject<List<Client>>(restResponse.Content);

            return View(clientList);
        }
    }
}
