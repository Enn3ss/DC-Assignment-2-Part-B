using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ClassLibrary;
using Newtonsoft.Json;
using RestSharp;
using WebServer.Models;
using Client = WebServer.Models.Client;
using Job = WebServer.Models.Job;
using System.Security.Cryptography;

namespace ClientDesktopApp
{
    public partial class MainWindow : Window
    {
        readonly ConsoleContent dc = new ConsoleContent();
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
                Thread.Sleep(new Random().Next(1000, 7000));

                if (JobLock.isLocked == false) // Preventing race condition
                {
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
                                this.SetProgressBarVisible();
                                JobLock.isLocked = true;
                                // Completing the job and getting result
                                string result = jobServer.CompleteJob(Decode(job.PythonCode)); // Decoding
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
                                List<Job> newJobs = jobServer.GetJobs();                              
                                JobLock.isLocked = false;
                                Thread.Sleep(2000);
                                this.SetProgressBarHidden();
                                this.UpdateAvailableJobs(newJobs);

                                break;
                            }
                        }
                    }
                }
            }
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
            host.Open(); // Open the host but never explicitly close; implicitly close upon Client GUI close
            dc.ConsoleInput = "System Online!";
            dc.RunCommand();

            while (true) { }
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
                PythonCode = Encode(PythonCodeTextBox.Text)
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
        private string Encode(string data) // Base 64 Encoding
        {
            if (String.IsNullOrEmpty(data))
            {
                return data;
            }

            byte[] textBytes = Encoding.UTF8.GetBytes(data);

            return Convert.ToBase64String(textBytes);
        }

        private string Decode(string base64Data) // Base 64 Decoding
        {
            if (String.IsNullOrEmpty(base64Data))
            {
                return base64Data;
            }

            byte[] encodedBytes = Convert.FromBase64String(base64Data);

            return Encoding.UTF8.GetString(encodedBytes);
        }

        protected override void OnClosed(EventArgs e) // Upon close delete client from db
        {
            base.OnClosed(e);
            DeregisterClient();
        }

        private void DeregisterClient()
        {
            RestClient restClient = new RestClient(localHost);
            RestRequest restRequest = new RestRequest("api/Clients/{id}", Method.Delete);
            restRequest.AddUrlSegment("id", clientId);
            RestResponse restResponse = restClient.Execute(restRequest);

            Client client = JsonConvert.DeserializeObject<Client>(restResponse.Content);

            if (client != null)
            {
                //no error
            }
            else
            {
                //error
            }
        }

        private void UpdateAvailableJobs(List<Job> jobs) // Updates ListBox GUI from another thread
        {
            Dispatcher.Invoke(() =>
            {
                JobListBox.Items.Clear();

                foreach (Job job in jobs)
                {
                    if (job.ClientId != clientId)
                    {
                        JobListBox.Items.Add(Decode(job.PythonCode));
                    }
                }
            });
        }

        private void UpdateResult(string result) // Updates last job result on GUI from another thread
        {
            Dispatcher.Invoke(() =>
            {
                ResultTextBlock.Text = "Result of Last Completed Job: " + result;
            });
        }

        private void UpdateJobsCompleted() // Updates number of jobs completed on GUI from another thread
        {
            Dispatcher.Invoke(() =>
            {
                JobsCompletedTextBlock.Text = "Completed Jobs: " + completedJobs.ToString();
            });
        }

        private void SetProgressBarVisible() // Sets progress bar visible from another thread
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Visibility = Visibility.Visible;
            });          
        }

        private void SetProgressBarHidden() // Sets progress bar invisible from another thread
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Visibility = Visibility.Hidden;
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
