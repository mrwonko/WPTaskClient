using System; // makes IAsyncOperation await-able
using System.Security.Cryptography;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Certificates;
using Windows.Security.Cryptography.Core;

namespace WPTaskClient.Protocol
{
    class RSA
    {
        public static async Task<Certificate> ReadCertificate()
        {
            // FIXME: should not use system certificate store, it prevents submissions to Windows Store
            foreach(var cert in await CertificateStores.FindAllAsync())
            {
                // TODO: let user choose a cert
                if(cert.HasPrivateKey)
                {
                    return cert; // works on my machine :P
                }
            }
            return null;
        }
    }
}
