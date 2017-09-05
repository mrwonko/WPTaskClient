using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
            foreach(var task in tasks)
            {
                this.tasks.Add(task);
            }
        }
    }
}
