using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

            private readonly Guid uuid;
            private readonly Status status;
            private readonly String description;
            private readonly Timestamp entered;
            private readonly Timestamp lastModified;
            private readonly ImmutableList<string> tags;
            private static readonly ImmutableHashSet<string> handledAttributes =
                ImmutableHashSet.Create("uuid", "status", "description", "entry", "modified", "tags");
            private readonly ImmutableDictionary<string, Json.IJsonValue> additionalAttributes;

            private static ImmutableList<string> noTags =
                ImmutableList.Create<string>();
            private static ImmutableDictionary<string, Json.IJsonValue> noAdditionalAttributes =
                ImmutableDictionary.Create<string, Json.IJsonValue>();

            public Task(Guid uuid, Status status, string description, Timestamp entered, Timestamp lastModified, ImmutableList<string> tags, ImmutableDictionary<string, Json.IJsonValue> additionalAttributes)
            {
                if (tags.Any(tag => tag.Any(char.IsWhiteSpace)))
                {
                    throw new System.ArgumentException("tag contains whitespace", "tags");
                }
                this.uuid = uuid;
                this.status = status;
                this.description = description;
                this.entered = entered;
                this.lastModified = lastModified;
                this.tags = tags;
                this.additionalAttributes = additionalAttributes;
            }

            public static Task New(string description, ImmutableList<string> tags)
            {
                var now = Timestamp.Now;
                return new Task(Guid.NewGuid(), Status.Pending, description, now, now, tags, noAdditionalAttributes);
            }

            public static Task FromJson(Json.JsonObject json)
            {
                var uuid = Guid.Parse(json.GetNamedString("uuid"));
                var status = statusLUT[json.GetNamedString("status")];
                var description = json.GetNamedString("description");
                var entered = new Timestamp(json.GetNamedString("entry"));
                var lastModified = new Timestamp(json.GetNamedString("modified"));
                var tags = noTags;
                if (json.ContainsKey("tags"))
                {
                    tags = ImmutableList.CreateRange(json.GetNamedArray("tags").Select(value => value.GetString()));
                }
                var additionalAttributes = ImmutableDictionary.CreateRange(json.Where(entry => !handledAttributes.Contains(entry.Key)));
                return new Task(uuid, status, description, entered, lastModified, tags, additionalAttributes);
            }

            public Json.JsonObject ToJson()
            {
                var result = new Json.JsonObject
                {
                    { "uuid", Json.JsonValue.CreateStringValue(uuid.ToString()) },
                    { "status", Json.JsonValue.CreateStringValue(status.ToString().ToLower()) },
                    { "description", Json.JsonValue.CreateStringValue(description) },
                    { "entry", Json.JsonValue.CreateStringValue(entered.ToString()) },
                    { "modified", Json.JsonValue.CreateStringValue(lastModified.ToString()) }
                };
                if (!tags.IsEmpty)
                {
                    var tags = new Json.JsonArray();
                    this.tags.ForEach(tag => tags.Add(Json.JsonValue.CreateStringValue(tag)));
                    result.Add("tags", tags);
                }
                foreach (var attribute in this.additionalAttributes)
                {
                    result.Add(attribute.Key, attribute.Value);
                }
                return result;
            }
        }
    }
}