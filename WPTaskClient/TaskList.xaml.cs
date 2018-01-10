using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography.Certificates;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using WPTaskClient.Extensions;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WPTaskClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TaskListPage : Page
    {
        public TaskListPage()
        {
            this.InitializeComponent();
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        private ObservableCollection<Data.Task> tasks = new ObservableCollection<Data.Task>() { Data.Task.New("loading...", ImmutableList.Create<string>()) };

        public ObservableCollection<Data.Task> Tasks { get { return tasks; } }

        private async void ButtonSync_Click(object sender, RoutedEventArgs e)
        {
            using (new ControlDisabler(ButtonSync))
            {
                var settings = Storage.Settings.Load();
                if (!settings.Valid)
                {
                    await new ErrorContentDialog("Sync not configured, please adjust settings.").ShowAsync();
                    return;
                }
                var certs = await CertificateStores.FindAllAsync(new CertificateQuery { FriendlyName = Constants.ClientCertFriendlyName });
                var cert = certs.Where(c => c.HasPrivateKey).FirstOrDefault();
                if (cert == null)
                {
                    await new ErrorContentDialog("No client certificate configured, please adjust settings.").ShowAsync();
                    return;
                }
                using (var stream = new StreamSocket())
                {
                    stream.Control.ClientCertificate = cert;
                    // TODO: Handle exceptions
                    try
                    {
                        await stream.ConnectAsync(settings.Endpoint, SocketProtectionLevel.Tls12).AsTask().WithTimeout(Constants.SyncTimeoutMillis);
                        // TODO: listen for response in background, see https://docs.microsoft.com/en-us/windows/uwp/launch-resume/support-your-app-with-background-tasks & https://docs.microsoft.com/en-us/windows/uwp/networking/network-communications-in-the-background
                        var request = new Protocol.Message(new Dictionary<string, string>
                        {
                            { "type", "statistics" },
                            { "org", settings.Organization },
                            { "user", settings.User },
                            { "key", settings.Key },
                            { "client", "WPTaskClient 0.1.0" },
                            { "protocol", "v1" },
                        }, "");
                        await request.ToStream(stream.OutputStream.AsStreamForWrite()).WithTimeout(Constants.SyncTimeoutMillis);
                        var response = await Protocol.Message.FromStream(stream.InputStream.AsStreamForRead()).WithTimeout(Constants.SyncTimeoutMillis);
                    }
                    catch (TimeoutException)
                    {
                        await new ErrorContentDialog("timeout during sync").ShowAsync();
                    }
                }
            }
        }

        private void ButtonNew_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(NewTaskPage));
        }

        async protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // TODO handle failure
            await UpdateTasks();
        }

        private bool updatingTasks = false;
        async private Task UpdateTasks()
        {
            if (updatingTasks) { return; };
            updatingTasks = true;
            try
            {
                var tasks = await Storage.SqliteStorage.GetTasks();
                // TODO: is there a simpler way?
                this.tasks.Clear();
                foreach (var task in tasks)
                {
                    if (task.Status != Data.TaskStatus.Deleted && task.Status != Data.TaskStatus.Completed)
                    {
                        this.tasks.Add(task);
                    }
                }
            }
            finally
            {
                updatingTasks = false;
            }
        }
    }
}
