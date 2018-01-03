using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WPTaskClient.Protocol;

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
            this.ButtonSync.IsEnabled = false;
            var endpoint = new EndpointPair(null, "", new HostName("mrwonko.de"), "53589");
            using (var stream = new StreamSocket())
            {
                stream.Control.ClientCertificate = await RSA.ReadCertificate();
                // TODO: Handle exceptions
                // TODO: add timeout https://docs.microsoft.com/en-us/previous-versions/windows/apps/jj710176(v=win.10)
                await stream.ConnectAsync(endpoint, SocketProtectionLevel.Tls12);
                // TODO: listen for response in background, see https://docs.microsoft.com/en-us/windows/uwp/launch-resume/support-your-app-with-background-tasks & https://docs.microsoft.com/en-us/windows/uwp/networking/network-communications-in-the-background
            }
            this.ButtonSync.IsEnabled = true;
        }

        private void ButtonNew_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(NewTaskPage));
        }

        async protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // TODO handle failure
            var tasks = await Storage.SqliteStorage.GetTasks();
            // TODO: is there a simpler way?
            this.tasks.Clear();
            foreach (var task in tasks)
            {
                this.tasks.Add(task);
            }
        }
    }
}
