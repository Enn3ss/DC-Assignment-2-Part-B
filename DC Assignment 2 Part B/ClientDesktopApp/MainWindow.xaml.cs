using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Newtonsoft.Json;
using RestSharp;
using WebServer.Models;

namespace ClientDesktopApp
{
    public partial class MainWindow : Window
    {
        ConsoleContent dc = new ConsoleContent();
        readonly Thread networkingThread;
        readonly Thread serverThread;
        private JobServerInterface jobServer;
        readonly int clientId;
        readonly string ipAddress = "127.0.0.1";
        readonly string port;
        int completedJobs = 0;
        readonly string localHost = "http://localhost:52998/";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = dc;
            Loaded += MainWindow_Loaded;

            // Generating random client ID + port number
            Random rnd = new Random();
            clientId = rnd.Next();
            port = rnd.Next(8100, 65536).ToString();

            // Starting server thread
            serverThread = new Thread(new ThreadStart(this.ServerThreadTask));
            serverThread.IsBackground = true;
            serverThread.Start();

            // Starting networking thread
            networkingThread = new Thread(new ThreadStart(this.NetworkingThreadTask));
            networkingThread.IsBackground = true;
            networkingThread.Start();

            // Adding current client to database
            RegisterClient();
            ClientIdTextBlock.Text = "Client ID: " + clientId.ToString();
            IpAddressTextBlock.Text = "IP Address: " + ipAddress;
            PortTextBlock.Text = "Port: " + port;
            JobsCompletedTextBlock.Text = "Completed Jobs: " + completedJobs.ToString();
        }

        private void NetworkingThreadTask()
        {
            ChannelFactory<JobServerInterface> foobFactory;
            NetTcpBinding tcp = new NetTcpBinding();

            string URL = "net.tcp://localhost:" + port + "/JobService";
            foobFactory = new ChannelFactory<JobServerInterface>(tcp, URL);
            jobServer = foobFactory.CreateChannel();

            
            while (true)
            {
                Thread.Sleep(2000);
                List<Job> jobs = jobServer.GetJobs();
                this.UpdateAvailableJobs(jobs);
            
                if (jobs != null) // Check if job list is null
                {
                    var rnd = new Random();
                    var randomized = jobs.OrderBy(item => rnd.Next()); // Shuffling list of jobs in random order

                    foreach (var job in randomized)
                    {
                        if (job.ClientId != clientId)
                        {
                            // Completing the job and getting result
                            string result = jobServer.CompleteJob(job.PythonCode);
                            // Updating the job and client
                            completedJobs++;
                            Client updatedClient = new Client
                            {
                                Id = clientId,
                                IPAddress = ipAddress,
                                Port = port,
                                JobsFinished = completedJobs
                            };
                            jobServer.UpdateJob(job, updatedClient);
                            // Update GUI
                            this.UpdateResult(result);
                            this.UpdateJobsCompleted();

                            break;
                        }
                    }
                }
                else
                {
                    // Error
                }

                Thread.Sleep(1000);
            }

            
            /*Dispatcher.BeginInvoke(new Action(() =>
            {
                while (true)
                {
                    // look for new clients
                    // check each client for jobs

                    List<Job> jobs = jobServer.GetJobs();
                    //jobList = new List<string>();

                    foreach (Job job in jobs)
                    {
                        if (job.ClientId != clientId)
                        {
                            //jobList.Add(job.PythonCode.ToString());
                            JobListBox.Items.Add("Test");
                        }
                    }

                    //JobList.ItemsSource = jobList;

                    Thread.Sleep(1000);
                }
            }));*/
        }

        private void ServerThreadTask()
        {
            dc.ConsoleInput = "Starting the Server Thread...";
            dc.RunCommand();
            ServiceHost host;
            NetTcpBinding tcp = new NetTcpBinding();
            string connection = "net.tcp://" + ipAddress + ":" + port + "/JobService";

            host = new ServiceHost(typeof(JobServer));
            host.AddServiceEndpoint(typeof(JobServerInterface), tcp, connection);
            host.Open();
            dc.ConsoleInput = "System Online!";
            dc.RunCommand();

            while (true)
            {

            }

            //host.Close();
        }
        
        private void RegisterClient()
        {
            Client thisClient = new Client
            {
                Id = clientId,
                IPAddress = ipAddress,
                Port = port,
                JobsFinished = completedJobs
            };

            RestClient client = new RestClient(localHost);
            RestRequest request = new RestRequest("api/Clients", Method.Post);
            request.AddJsonBody(JsonConvert.SerializeObject(thisClient));
            RestResponse response = client.Execute(request);

            Client returnClient = JsonConvert.DeserializeObject<Client>(response.Content);

            if (returnClient != null)
            {
                dc.ConsoleInput = "Client successfully added to the web server!";
                dc.RunCommand();
            }
            else
            {
                dc.ConsoleInput = "Error: " + response.Content;
                dc.RunCommand();
            }
        }
        private void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            Job newJob = new Job
            {
                Id = new Random().Next(),
                ClientId = clientId,
                PythonCode = PythonCodeTextBox.Text
            };

            RestClient client = new RestClient(localHost);
            RestRequest request = new RestRequest("api/Jobs", Method.Post);
            request.AddJsonBody(JsonConvert.SerializeObject(newJob));
            RestResponse response = client.Execute(request);

            Job returnJob = JsonConvert.DeserializeObject<Job>(response.Content);

            if (returnJob != null)
            {
                dc.ConsoleInput = "Job successfully added to the web server!";
                dc.RunCommand();
            }
            else
            {
                dc.ConsoleInput = "Error: " + response.Content;
                dc.RunCommand();
            }
        }

        private void UpdateAvailableJobs(List<Job> jobs)
        {
            Dispatcher.Invoke(() =>
            {
                JobListBox.Items.Clear();

                foreach (Job job in jobs)
                {
                    if (job.ClientId != clientId)
                    {
                        JobListBox.Items.Add(job.PythonCode);
                    }
                }
            });
        }

        private void UpdateResult(string result)
        {
            Dispatcher.Invoke(() =>
            {
                ResultTextBlock.Text = "Result of Last Completed Job: " + result;
            });
        }

        private void UpdateJobsCompleted()
        {
            Dispatcher.Invoke(() =>
            {
                JobsCompletedTextBlock.Text = "Completed Jobs: " + completedJobs.ToString();
            });
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InputBlock.KeyDown += InputBlock_KeyDown;
            InputBlock.Focus();
        }

        void InputBlock_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                dc.ConsoleInput = InputBlock.Text;
                dc.RunCommand();
                InputBlock.Focus();
                Scroller.ScrollToBottom();
            }
        }
    }

    public class ConsoleContent : INotifyPropertyChanged
    {
        string consoleInput = string.Empty;
        ObservableCollection<string> consoleOutput = new ObservableCollection<string>() { "NET Remoting Server" };

        public string ConsoleInput
        {
            get
            {
                return consoleInput;
            }
            set
            {
                consoleInput = value;
                OnPropertyChanged("ConsoleInput");
            }
        }

        public ObservableCollection<string> ConsoleOutput
        {
            get
            {
                return consoleOutput;
            }
            set
            {
                consoleOutput = value;
                OnPropertyChanged("ConsoleOutput");
            }
        }

        public void RunCommand()
        {
            ConsoleOutput.Add(ConsoleInput);
            // do your stuff here.
            ConsoleInput = String.Empty;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
