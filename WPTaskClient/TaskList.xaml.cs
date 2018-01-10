using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
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

        private Regex GUIDRegex = new Regex("^[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}$", RegexOptions.Compiled);
        private async void ButtonSync_Click(object sender, RoutedEventArgs args)
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
                var syncBacklog = await Storage.SqliteStorage.GetSyncBacklog();
                // FIXME: this whole initial sync special case is very ugly - surely there's an easier way to do it in an idempotent way?
                var isInitialSync = syncBacklog.SyncKey == null;
                var initialSyncPhase = 0;
                // TODO: Handle exceptions
                while (true)
                {
                    Protocol.Message response;
                    using (var stream = new StreamSocket())
                    {
                        stream.Control.ClientCertificate = cert;
                        try
                        {
                            await stream.ConnectAsync(settings.Endpoint, SocketProtectionLevel.Tls12).AsTask().WithTimeout(Constants.SyncTimeoutMillis);
                            // TODO: listen for response in background, see https://docs.microsoft.com/en-us/windows/uwp/launch-resume/support-your-app-with-background-tasks & https://docs.microsoft.com/en-us/windows/uwp/networking/network-communications-in-the-background
                            var request = new Protocol.Message(new Dictionary<string, string>
                        {
                            { "type", "sync" },
                            { "org", settings.Organization },
                            { "user", settings.User },
                            { "key", settings.Key },
                            { "client", "WPTaskClient 0.1.0" },
                            { "protocol", "v1" },
                        }, isInitialSync && initialSyncPhase == 0 ? "" : syncBacklog.ToString()); // must not send local changes on initial sync
                            await request.ToStream(stream.OutputStream.AsStreamForWrite()).WithTimeout(Constants.SyncTimeoutMillis);
                            response = await Protocol.Message.FromStream(stream.InputStream.AsStreamForRead()).WithTimeout(Constants.SyncTimeoutMillis);
                        }
                        catch (TimeoutException)
                        {
                            await new ErrorContentDialog("timeout during sync").ShowAsync();
                            return;
                        }
                    }
                    if (!response.Header.TryGetValue("code", out string code))
                    {
                        await new ErrorContentDialog("No status in sync response!").ShowAsync();
                        return;
                    }
                    if (code == "201") // no change
                    {
                        return;
                    }
                    if (code != "200")
                    {
                        if (response.Header.TryGetValue("status", out string status))
                        {
                            await new ErrorContentDialog(string.Format("Sync response had status {0}: {1}!", code, status)).ShowAsync();
                        }
                        else
                        {
                            await new ErrorContentDialog(string.Format("Sync response had status {0}!", code)).ShowAsync();
                        }
                        return;
                    }
                    string syncKey = null;
                    try
                    {
                        foreach (var line in response.Body.Split('\n')) // I wish there was an easy lazy way to do this (a lazy lazy way, if you will)
                        {
                            if (line.Length == 0)
                            {
                                continue;
                            }
                            if (line[0] == '{')
                            {
                                Data.Task task;
                                try
                                {
                                    task = Data.Task.FromJson(JsonObject.Parse(line));
                                }
                                catch (Exception e)
                                {
                                    await new ErrorContentDialog(e).ShowAsync();
                                    return;
                                }
                                // TODO: Batch
                                await Storage.SqliteStorage.UpsertTask(task, false);
                            }
                            else if (GUIDRegex.IsMatch(line))
                            {
                                syncKey = line;
                            }
                            else
                            {
                                await new ErrorContentDialog(string.Format("unexpected line: {0}", line)).ShowAsync();
                                return;
                            }
                        }
                    }
                    finally
                    {
                        await UpdateTasks();
                    }
                    if (syncKey == null)
                    {
                        await new ErrorContentDialog("Sync response contained no sync key!").ShowAsync();
                        return;
                    }
                    await Storage.SqliteStorage.SetSyncKey(syncKey);
                    syncBacklog.SyncKey = syncKey;

                    if (isInitialSync && initialSyncPhase < 1)
                    {
                        initialSyncPhase++;
                        // run again, this time syncing our initial local changes
                    }
                    else
                    {
                        break;
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
                    if (task.Status == Data.TaskStatus.Pending)
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
