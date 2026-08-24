# sqlite-net-base

sqlite-net-base is the provider-neutral SQLite-net package. It contains the synchronous and asynchronous SQLite APIs, but it does not select or bundle a native SQLite provider.

Use sqlite-net-pcl instead unless the application needs to choose and configure its own SQLitePCLRaw provider.

## Install and configure a provider

Install sqlite-net-base together with a SQLitePCLRaw provider and its native SQLite library. For example, an application using e_sqlite3 can reference:

```xml
<PackageReference Include="sqlite-net-base" Version="..." />
<PackageReference Include="SQLitePCLRaw.provider.e_sqlite3" Version="..." />
<PackageReference Include="SourceGear.sqlite3" Version="..." />
```

Configure the provider once during application startup, before creating a connection:

```csharp
SQLitePCL.raw.SetProvider (new SQLitePCL.SQLite3Provider_e_sqlite3 ());
```

The provider and native library packages depend on the target operating systems and deployment model. Applications using a platform-provided SQLite library can select the corresponding SQLitePCLRaw provider instead.

## Define a table

```csharp
using SQLite;

public class TodoItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string Title { get; set; }

    public bool Completed { get; set; }
}
```

## Synchronous API

```csharp
var databasePath = Path.Combine (AppContext.BaseDirectory, "app.db3");

using var db = new SQLiteConnection (databasePath);
db.CreateTable<TodoItem> ();

var item = new TodoItem { Title = "Read the documentation" };
db.Insert (item);

item.Completed = true;
db.Update (item);

var completedItems = db.Table<TodoItem> ()
    .Where (x => x.Completed)
    .ToList ();

db.Delete (item);
```

Use parameter placeholders for raw SQL values:

```csharp
var items = db.Query<TodoItem> (
    "select * from TodoItem where Title = ?",
    "Read the documentation");
```

## Asynchronous API

```csharp
var connectionString = new SQLiteConnectionString (databasePath, true);
var db = new SQLiteAsyncConnection (connectionString);

await db.CreateTableAsync<TodoItem> ();

var item = new TodoItem { Title = "Use the async API" };
await db.InsertAsync (item);

var items = await db.Table<TodoItem> ()
    .Where (x => !x.Completed)
    .ToListAsync ();

await db.CloseAsync ();
```

## Native AOT-safe JSON columns

Complex objects can be stored as JSON by applying StoreAsJson and providing a source-generated JsonSerializerContext. Reflection-based JSON serialization is not used, which keeps the mapping compatible with trimming and Native AOT.

```csharp
using System.Text.Json.Serialization;
using SQLite;

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
}

public class CustomerProfile
{
    public string DisplayName { get; set; }
    public Address Address { get; set; }
}

[JsonSerializable (typeof (CustomerProfile))]
public partial class AppJsonContext : JsonSerializerContext
{
}

public class Customer
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [StoreAsJson (typeof (AppJsonContext))]
    public CustomerProfile Profile { get; set; }
}
```

The context must provide metadata for the exact property type. Referenced child objects, collections, and dictionaries are included transitively by System.Text.Json source generation.

```csharp
db.CreateTable<Customer> ();

db.Insert (new Customer {
    Profile = new CustomerProfile {
        DisplayName = "Ada",
        Address = new Address {
            Street = "10 Main Street",
            City = "London",
        },
    },
});
```

Unsupported complex properties without StoreAsJson are rejected instead of being serialized through reflection.

## Common mapping attributes

- Table: overrides the table name.
- Column: overrides a column name.
- PrimaryKey: identifies the primary key.
- AutoIncrement: generates integer primary keys.
- Indexed and Unique: create indexes.
- NotNull: creates a NOT NULL constraint.
- Ignore: excludes a property from persistence.
- StoreAsText: stores enum values as text.
- StoreAsJson: stores a complex property using source-generated JSON metadata.
