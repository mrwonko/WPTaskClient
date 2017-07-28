using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Json = Windows.Data.Json;

namespace WPTaskClient
{
    namespace Data
    {
        public class Task
        {
            public enum Status
            {
                Pending,
                Deleted,
                Completed,
                Waiting,
                Recurring,
            }
            
            private static readonly Dictionary<string, Status> statusLUT = new Dictionary<string, Status>
            {
                { "pending", Status.Pending },
                { "deleted", Status.Deleted },
                { "completed", Status.Completed },
                { "waiting", Status.Waiting },
                { "recurring", Status.Recurring },
            };

            private Guid uuid;
            private Status status;
            private String description;
            private Timestamp entered;
            private Timestamp lastModified;
            private static readonly HashSet<string> handledAttributes = new HashSet<string> { "uuid", "status", "description", "entry", "modified" };
            private Dictionary<string, Json.IJsonValue> additionalAttributes;

            public Task(Guid uuid, Status status, string description, Timestamp entered, Timestamp lastModified, Dictionary<string, Json.IJsonValue> additionalAttributes)
            {
                this.uuid = uuid;
                this.status = status;
                this.description = description;
                this.entered = entered;
                this.lastModified = lastModified;
                this.additionalAttributes = additionalAttributes;
            }

            public static Task New(string description)
            {
                var now = Timestamp.Now;
                return new Task(Guid.NewGuid(), Status.Pending, description, now, now, new Dictionary<string, Json.IJsonValue>());
            }

            public static Task FromJson(Json.JsonObject json)
            {
                var uuid = Guid.Parse(json.GetNamedString("uuid"));
                var status = statusLUT[json.GetNamedString("status")];
                var description = json.GetNamedString("description");
                var entered = new Timestamp(json.GetNamedString("entry"));
                var lastModified = new Timestamp(json.GetNamedString("modified"));
                var additionalAttributes = new Dictionary<string, Json.IJsonValue>();
                foreach (var entry in json)
                {
                    if (!handledAttributes.Contains(entry.Key))
                    {
                        additionalAttributes.Add(entry.Key, entry.Value);
                    }

                }
                return new Task(uuid, status, description, entered, lastModified, additionalAttributes);
            }

            public Json.JsonObject ToJson()
            {
                var result = new Json.JsonObject();
                result.Add("uuid", Json.JsonValue.CreateStringValue(uuid.ToString()));
                result.Add("status", Json.JsonValue.CreateStringValue(this.status.ToString().ToLower()));
                result.Add("description", Json.JsonValue.CreateStringValue(this.description));
                result.Add("entry", Json.JsonValue.CreateStringValue(this.entered.ToString()));
                result.Add("modified", Json.JsonValue.CreateStringValue(this.lastModified.ToString()));
                foreach (var attribute in this.additionalAttributes)
                {
                    result.Add(attribute.Key, attribute.Value);
                }
                return result;
            }
        }
    }
}