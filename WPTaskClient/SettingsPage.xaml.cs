using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WPTaskClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private string clientCert = null;
        private string clientCertPassphrase = null;

        public SettingsPage()
        {
            this.InitializeComponent();
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

        private async void ButtonClientCert_Click(object sender, RoutedEventArgs args)
        {
            using (new ControlDisabler(ButtonClientCert))
            {
                var picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.List,
                    SuggestedStartLocation = PickerLocationId.Downloads,
                };
                picker.FileTypeFilter.Add(".pfx");
                var file = await picker.PickSingleFileAsync();
                if (file == null)
                {
                    return;
                }
                var fileStream = await file.OpenReadAsync();
                var certData = await new StreamReader(fileStream.AsStreamForRead()).ReadToEndAsync();
                if (certData.IndexOf("-----BEGIN PKCS12-----") == -1)
                {
                    await new ErrorContentDialog("Selected file is not a base64 PKCS12 certificate. Please use a certificate created using `certtool --load-certificate user.cert.pem --load-privkey user.key.pem --to-p12 --outfile user.pfx`")
                        .ShowAsync();
                    return;
                }
                var passwordDialog = new CertPasswordContentDialog();
                if (await passwordDialog.ShowAsync() != ContentDialogResult.Primary)
                {
                    return;
                }
                var pass = passwordDialog.Password;
                try
                {
                    // we're importing it with a temporary name to see if it's valid; "real" importing doesn't happen until save is pressed.
                    await CertificateEnrollmentManager.ImportPfxDataAsync(certData, pass, ExportOption.NotExportable, KeyProtectionLevel.NoConsent, InstallOptions.None, Constants.ClientCertTempName);
                }
                catch (Exception e)
                {
                    await new ErrorContentDialog(e)
                        .ShowAsync();
                    return;
                }
                var certs = await CertificateStores.FindAllAsync(new CertificateQuery { FriendlyName = Constants.ClientCertTempName });
                if (!certs.Where(c => c.HasPrivateKey).Any())
                {
                    await new ErrorContentDialog("Selected certificate contains no private key. Please use a certificate created using `certtool --load-certificate user.cert.pem --load-privkey user.key.pem --to-p12 --outfile user.pfx`")
                        .ShowAsync();
                    return;
                }
                // everything looks good, remember values for saving
                clientCert = certData;
                clientCertPassphrase = pass;
            }
        }
    }
}
