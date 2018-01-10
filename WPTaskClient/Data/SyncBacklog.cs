using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPTaskClient.Data
{
    public class SyncBacklog
    {
        public string SyncKey;
        public IImmutableList<String> Tasks;

        public override string ToString()
        {
            var builder = new StringBuilder();
            if(SyncKey != null && SyncKey != "")
            {
                builder.Append(SyncKey);
                builder.Append('\n'); // not using AppendLine because we want LF, not CRLF -.-
            }
            foreach(var task in Tasks)
            {
                builder.Append(task);
                builder.Append('\n');
            }
            return builder.ToString();
        }
    }
}
