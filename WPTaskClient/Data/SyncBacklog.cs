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
        public IImmutableList<Task> Tasks;

        public override string ToString()
        {
            var builder = new StringBuilder();
            if(SyncKey != null && SyncKey != "")
            {
                builder.AppendLine(SyncKey);
            }
            foreach(var task in Tasks)
            {
                builder.AppendLine(task.ToJson().ToString());
            }
            return builder.ToString();
        }
    }
}
