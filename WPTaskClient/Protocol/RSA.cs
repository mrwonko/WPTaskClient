using System; // makes IAsyncOperation await-able
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Certificates;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Pickers;

namespace WPTaskClient.Protocol
{
    class RSA
    {
        public static async Task<Certificate> ReadCertificate()
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.FileTypeFilter.Add(".pfx");
            var file = await picker.PickSingleFileAsync();
            var fileStream = await file.OpenReadAsync();
            var contents = new MemoryStream();
            await fileStream.AsStreamForRead().CopyToAsync(contents);
            var pfxData = Encoding.ASCII.GetString(contents.ToArray());
            // TODO: verify it contains "-----BEGIN PKCS12-----" - apparently this does not work with binary representations
            var password = "TODO ask user";
            var friendlyName = "TaskClientCert";
            await CertificateEnrollmentManager.ImportPfxDataAsync(pfxData, password, ExportOption.NotExportable, KeyProtectionLevel.NoConsent, InstallOptions.None, friendlyName);
            
            //var appStore = CertificateStores.GetStoreByName(StandardCertificateStoreNames.Personal);
            var query = new CertificateQuery();
            query.FriendlyName = friendlyName;
            var certs = await CertificateStores.FindAllAsync(query);
            foreach (var cert in certs)
            {
                if(cert.HasPrivateKey)
                {
                    return cert;
                }
            }
            return null;
        }
    }
}
