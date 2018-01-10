
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Json = Windows.Data.Json;
using System.Collections.Immutable;

namespace WPTaskClientTests
{
    [TestClass]
    public class Task
    {
        [TestMethod]
        public void TestToJson()
        {
            // given
            var rawUUID = "296d835e-8f85-4224-8f36-c612cad1b9f8";
            var description = "test";
            var enteredDate = new WPTaskClient.Data.Timestamp(new DateTime(2017, 7, 25, 21, 29, 0));
            var modifiedDate = new WPTaskClient.Data.Timestamp(new DateTime(2017, 7, 28, 13, 25, 0));
            var tags = new List<string>() { "first", "second" }.ToImmutableList();
            var additionalAttributes = new Dictionary<string, Json.IJsonValue>()
            {
                { "estimate", Json.JsonValue.CreateStringValue("1w")}
            }.ToImmutableDictionary();
            // when
            var entry = new WPTaskClient.Data.Task(Guid.Parse(rawUUID), WPTaskClient.Data.TaskStatus.Pending, description, enteredDate, modifiedDate, tags, additionalAttributes);
            var json = entry.ToJson();
            // then
            var expectedJson = new Json.JsonObject()
            {
                { "uuid", Json.JsonValue.CreateStringValue(rawUUID) },
                { "status", Json.JsonValue.CreateStringValue("pending") },
                { "entry", Json.JsonValue.CreateStringValue( "20170725T212900Z") },
                { "modified", Json.JsonValue.CreateStringValue( "20170728T132500Z") },
                { "description", Json.JsonValue.CreateStringValue(description) },
                { "estimate", Json.JsonValue.CreateStringValue("1w") },
                { "tags", new Json.JsonArray(){ Json.JsonValue.CreateStringValue("first"), Json.JsonValue.CreateStringValue("second") } },
            };
            AssertJsonEqual(json, expectedJson);
        }

        [TestMethod]
        public void TestFromJson()
        {
            // given
            var rawJson = @"
            { ""uuid"":        ""296d835e-8f85-4224-8f36-c612cad1b9f8""
            , ""status"":      ""pending""
            , ""entry"":       ""20170725T221600Z""
            , ""modified"":    ""20170728T132500Z""
            , ""description"": ""test task""
            , ""tags"":        [ ""foo"", ""bar"" ]
            , ""uda"":         ""user-defined attribute""
            }";
            var givenJson = Json.JsonObject.Parse(rawJson);
            // when
            var entry = WPTaskClient.Data.Task.FromJson(givenJson);
            var json = entry.ToJson();
            // then
            AssertJsonEqual(givenJson, json);
        }

        private static void AssertJsonEqual(Json.IJsonValue lhs, Json.IJsonValue rhs, string path = "")
        {
            Assert.AreEqual(lhs.ValueType, rhs.ValueType, path);
            switch (lhs.ValueType)
            {
                case Json.JsonValueType.Null:
                    // null always equal
                    break;
                case Json.JsonValueType.Boolean:
                    Assert.AreEqual(lhs.GetBoolean(), rhs.GetBoolean(), path);
                    break;
                case Json.JsonValueType.Number:
                    Assert.AreEqual(lhs.GetNumber(), rhs.GetNumber(), path);
                    break;
                case Json.JsonValueType.String:
                    Assert.AreEqual(lhs.GetString(), rhs.GetString(), path);
                    break;
                case Json.JsonValueType.Array:
                    var lhsArr = lhs.GetArray();
                    var rhsArr = rhs.GetArray();
                    Assert.AreEqual(lhsArr.Count, rhsArr.Count, path + ".length");
                    for (var i = 0; i < lhsArr.Count; i++)
                    {
                        AssertJsonEqual(lhsArr[i], rhsArr[i], String.Format("{0}[{1}]", path, i));
                    }
                    break;
                case Json.JsonValueType.Object:
                    var lhsObj = lhs.GetObject();
                    var rhsObj = rhs.GetObject();
                    Assert.AreEqual(lhsObj.Count, rhsObj.Count, String.Format("len({0})", path));
                    foreach (var lhsEntry in lhsObj)
                    {
                        AssertJsonEqual(lhsEntry.Value, rhsObj[lhsEntry.Key]);
                    }
                    break;
            }

        }
    }
}
