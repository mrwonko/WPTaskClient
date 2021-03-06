﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WPTaskClient
{
    public sealed partial class NewTaskPage : Page
    {
        public NewTaskPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested +=
    OnBackRequested;
            // enable back-button even on Desktop for this page
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested -=
    OnBackRequested;
            // disable back-button again upon leaving this page
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            base.OnNavigatedFrom(e);
        }

        private void OnBackRequested(object sender,
   Windows.UI.Core.BackRequestedEventArgs e)
        {
            // Navigate back if possible, and if the event has not 
            // already been handled .
            if (Window.Current.Content is Frame rootFrame && rootFrame.CanGoBack && e.Handled == false)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }

        // TODO: user-defined tags
        private static readonly ImmutableList<string> defaultTags = new List<string>() { "new" }.ToImmutableList();

        async private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Handle exceptions on invalid task
            var task = Data.Task.New(taskDescription.Text, defaultTags);
            using (new ControlDisabler(saveButton))
            {
                // TODO: handle failure
                await Storage.SqliteStorage.UpsertTask(task);
            }
            if (Window.Current.Content is Frame rootFrame && rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
            }
            else
            {
                Frame.Navigate(typeof(TaskListPage));
            }
        }
    }
}
