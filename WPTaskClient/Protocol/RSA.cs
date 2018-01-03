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

        public static void ParseRSADERPrivateKey()
        {
            /* according to RFC 3447
            RSAPrivateKey ::= SEQUENCE {
                version           Version,
                modulus           INTEGER,  -- n
                publicExponent    INTEGER,  -- e
                privateExponent   INTEGER,  -- d
                prime1            INTEGER,  -- p
                prime2            INTEGER,  -- q
                exponent1         INTEGER,  -- d mod (p-1)
                exponent2         INTEGER,  -- d mod (q-1)
                coefficient       INTEGER,  -- (inverse of q) mod p
                otherPrimeInfos   OtherPrimeInfos OPTIONAL
            }
            Version ::= INTEGER { two-prime(0), multi(1) }
               (CONSTRAINED BY
               {-- version must be multi if otherPrimeInfos present --})
            */
            var version = new ASN1.Integer("version");
            // TODO
        }
    }
}
