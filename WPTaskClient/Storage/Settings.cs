using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;

namespace WPTaskClient.Storage
{
    class Settings
    {
        private string server;
        private string organization;
        private string user;
        private string key;

        public string Server { get => server ?? ""; set => server = value; }
        public string Organization { get => organization ?? ""; set => organization = value; }
        public string User { get => user ?? ""; set => user = value; }
        public string Key { get => key ?? ""; set => key = value; }

        public EndpointPair Endpoint
        {
            get
            {
                var colonPos = server.IndexOf(':');
                var domain = colonPos == -1 ? server : server.Substring(0, colonPos);
                var port = colonPos == -1 ? "53589" : server.Substring(colonPos + 1);
                return new EndpointPair(null, "", new HostName(domain), port);
            }
        }

        public bool Valid
        {
            get
            {
                return server?.Length > 0
                    && organization?.Length > 0
                    && user?.Length > 0
                    && key?.Length > 0;
            }
        }

        public static Settings Load()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            return new Settings {
                server = (string)localSettings.Values[nameof(server)],
                organization = (string)localSettings.Values[nameof(organization)],
                user = (string)localSettings.Values[nameof(user)],
                key = (string)localSettings.Values[nameof(key)]
                };
        }

        public void Store()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[nameof(server)] = server;
            localSettings.Values[nameof(organization)] = organization;
            localSettings.Values[nameof(user)] = user;
            localSettings.Values[nameof(key)] = key;
        }
    }
}
