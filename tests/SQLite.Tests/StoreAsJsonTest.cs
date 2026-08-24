using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using NUnit.Framework;

namespace SQLite.Tests
{
	public class JsonValue
	{
		public string Name { get; set; }
		public int Count { get; set; }
		public List<string> Tags { get; set; }
	}

	public class MissingJsonValue
	{
		public string Value { get; set; }
	}

	[JsonSerializable (typeof (JsonValue))]
	public partial class StoreAsJsonContext : JsonSerializerContext
	{
	}

	[TestFixture]
	public class StoreAsJsonTest
	{
		public class JsonEntity
		{
			[PrimaryKey]
			public int Id { get; set; }

			[StoreAsJson (typeof (StoreAsJsonContext))]
			public JsonValue Value { get; set; }
		}

		public class MissingMetadataEntity
		{
			[PrimaryKey]
			public int Id { get; set; }

			[StoreAsJson (typeof (StoreAsJsonContext))]
			public MissingJsonValue Value { get; set; }
		}

		[Test]
		public void RoundTripsWithSourceGeneratedMetadata ()
		{
			using (var db = new TestDb ()) {
				db.CreateTable<JsonEntity> ();

				var mapping = db.GetMapping (typeof (JsonEntity));
				var valueColumn = mapping.Columns.Single (x => x.PropertyName == nameof (JsonEntity.Value));
				Assert.AreEqual ("varchar", Orm.SqlType (valueColumn, db.StoreDateTimeAsTicks, db.StoreTimeSpanAsTicks));

				var entity = new JsonEntity {
					Id = 1,
					Value = new JsonValue {
						Name = "first",
						Count = 2,
						Tags = new List<string> { "a", "b" },
					},
				};

				Assert.AreEqual (1, db.Insert (entity));
				Assert.That (db.ExecuteScalar<string> ("select Value from JsonEntity"), Does.Contain ("\"Name\":\"first\""));

				var loaded = db.Get<JsonEntity> (entity.Id);
				Assert.AreEqual ("first", loaded.Value.Name);
				Assert.AreEqual (2, loaded.Value.Count);
				CollectionAssert.AreEqual (new[] { "a", "b" }, loaded.Value.Tags);

				loaded.Value.Count = 3;
				Assert.AreEqual (1, db.Update (loaded));
				Assert.AreEqual (3, db.Get<JsonEntity> (entity.Id).Value.Count);
			}
		}

		[Test]
		public void RejectsContextWithoutPropertyMetadata ()
		{
			using (var db = new TestDb ()) {
				var exception = Assert.Throws<InvalidOperationException> (() => db.GetMapping (typeof (MissingMetadataEntity)));
				Assert.That (exception.Message, Does.Contain (typeof (MissingJsonValue).ToString ()));
			}
		}
	}
}
