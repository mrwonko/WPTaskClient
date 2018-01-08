﻿using System;
using System.Collections.Generic;
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

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WPTaskClient
{
    public sealed partial class ErrorContentDialog : ContentDialog
    {
        private ErrorContentDialog()
        {
            this.InitializeComponent();
        }

        public ErrorContentDialog(string message) : this()
        {
            this.errorText.Text = message;
        }

        public ErrorContentDialog(Exception exception) : this(exception.GetType().Name + ": " + exception.Message)
        {
        }
    }
}