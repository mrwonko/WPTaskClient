
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Json = Windows.Data.Json;

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
            var expectedEnteredDate = "20170728T132500Z";
            var expectedModifiedDate = "20170725T212900Z";
            var additionalAttributes = new Dictionary<string, Json.IJsonValue>
            {
                { "estimate", Json.JsonValue.CreateStringValue("1w") }
            };
            // when
            var entry = new WPTaskClient.Data.Task(Guid.Parse(rawUUID), WPTaskClient.Data.Task.Status.Pending, description, enteredDate, modifiedDate, additionalAttributes);
            var json = entry.ToJson();
            // then
            AssertStringValue(json, "uuid", rawUUID);
            AssertStringValue(json, "status", "pending");
            AssertStringValue(json, "entry", expectedEnteredDate);
            AssertStringValue(json, "modified", expectedModifiedDate);
            AssertStringValue(json, "description", description);
            AssertStringValue(json, "estimate", "1w");
        }

        [TestMethod]
        public void TestFromJson()
        {
            // given
            var rawJson = @"{""uuid"":""296d835e-8f85-4224-8f36-c612cad1b9f8"",""status"":""pending"",""entry"":""20170725T221600Z"",""modified"":""20170728T132500Z"",""description"":""test task"",""uda"":""user-defined attribute""}";
            var givenJson = Json.JsonObject.Parse(rawJson);
            // when
            var entry = WPTaskClient.Data.Task.FromJson(givenJson);
            var json = entry.ToJson();
            // then
            AssertJsonEqual(givenJson, json);
        }

        private static void AssertStringValue(Json.JsonObject obj, string field, string expected)
        {
            Assert.IsTrue(obj.TryGetValue(field, out Json.IJsonValue value), "object {0} must contain [1] field", obj, field);
            StringAssert.Equals(value.GetString(), expected);
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
