# SimpleDocumentStore
Easy way to store objects as documents without messing with any SQL or raw files.

The solution consists of a generic interface that can be used by the application. The interface can be backed by different storage solutions. Currently the solution supports raw files, MS SQL Server and PostgreSql as backing options. An in-memory implementation is also provided for testing purposes.

### Purpose
The main purpose with this is not to be used in large production systems but more during development or small simple applications. When developing, focus should be on the requirements and behavior not on data.
Following a clean architecture the data store can be stubbed by in-memory implementation but sometimes it is still nice to be able to save state between executions.

By using this you will have real persistency without any effort, this gives you time to fully implement your solution while postponing the desision of the actual data storage solution until it is actually needed.
In some cases you may even find that using this is in fact a sufficient storeage solution. Especially in small application with few and simple requirements.

#### Functionality

The following is supported:
- Create new objects in the store
- Update existing objects in the store
- Get objects from the store by Id
- Get all objects of a given type from the store
- Delete objects from the store by Id
- Delete all objects of a give type from the store
- Control how objects are serialized and deserialized
- File implementation will automatically create required directories
- Sql Server and PostgreSQL implementations will automatically create required database, schemas and tables

Some stuff that might be important but NOT supported:
- Transactions
- Indexing except for index on Id
- Searching for anything else than Id
- Handling of relations between objects
- Probably more...

### Implementations

#### In-memory
This can be used to easily mock out the actual storage to speed up execution in e.g. unit tests.

#### Files
Stores the objects as files. Each object type has a dedicated folder with one file per object. The object id is used for the filename making it very easy to navigate the filesystem and identify the relevant files.

#### SQL Server
Uses MS SQL Server as backing database. Each object type has a dedicated table with one row per object. The required database and tables is automatically created.

#### PostgreSql
Uses PostgreSql as backing database. Each object type has a dedicated table with one row per object. The required database and tables is automatically created.

### Usage

To add to your project, add as a nuget package for file store:

```
dotnet package add Basses.SimpleDocumentStore
```

or for MS SQL Server

```
dotnet package add Basses.SimpleDocumentStore.SqlServer
```

or for PostgreSQL

```
dotnet package add Basses.SimpleDocumentStore.PostgreSql
```

Or just copy the required files to your own project. This also allows for local changes if required.


### Examples

For examples of usage see the solution:

SimpleObjectDb
- Console application that show examples of usage of all methods and how to control serialization.

SimpleObjectDbWebApi
- Web API that shows how the document store can be used with DI and how it can be used in different modules by defining the schema

To run the eaxmples spin up docker with the required databases using docker compose:

```
docker-compose up
```

#### Performance

The SimpleObjectDb example application can execute some actions against the different database implementations and log the execution times. The actual results will of cause depend a lot on the machine running the application but the test will at least give some hints to how the performance is.

On my machine the output looks like this:

```
Creating test objects...
Created small object: 10000 of 10000
Created large object: 10 of 10
Objects created in...                                       00:00:05.0529209
Setting up store options...                                 00:00:00.4508779


----------------------------------------------------------------------------
Starting performance test of: InMemoryDocumentStore
----------------------------------------------------------------------------
Executing tests...
Creating 10000 small items in db...                         00:00:00.1010524
Fetching 10000 small items from db one by one...            00:00:00.0851349
Fetching 10000 small items from db at once...               00:00:00.0659028
Updating 10000 small items in db one by one...              00:00:00.0255639
Deleting 10000 small items from db one by one...            00:00:00.0015880
Creating 10 large items in db...                            00:00:00.0098042
Updating 10 large items in db one by one...                 00:00:00.0045090
Deleting 10 large items from db one by one...               00:00:00.0001612
Total execution time...                                     00:00:03.3944320


----------------------------------------------------------------------------
Starting performance test of: FileDocumentStore
----------------------------------------------------------------------------
Executing tests...
Creating 10000 small items in db...                         00:00:03.9213612
Fetching 10000 small items from db one by one...            00:00:02.4159106
Fetching 10000 small items from db at once...               00:00:01.8215434
Updating 10000 small items in db one by one...              00:00:03.3338303
Deleting 10000 small items from db one by one...            00:00:00.7678190
Creating 10 large items in db...                            00:00:00.7777533
Updating 10 large items in db one by one...                 00:00:00.0129387
Deleting 10 large items from db one by one...               00:00:00.0024588
Total execution time...                                     00:00:15.3159066


----------------------------------------------------------------------------
Starting performance test of: SqlServerDocumentStore
----------------------------------------------------------------------------
Executing tests...
Creating 10000 small items in db...                         00:00:58.3278140
Fetching 10000 small items from db one by one...            00:00:27.7637228
Fetching 10000 small items from db at once...               00:00:00.0528543
Updating 10000 small items in db one by one...              00:01:26.9920918
Deleting 10000 small items from db one by one...            00:01:06.6103734
Creating 10 large items in db...                            00:01:06.7006505
Updating 10 large items in db one by one...                 00:00:00.1330521
Deleting 10 large items from db one by one...               00:00:00.0566897
Total execution time...                                     00:04:03.0527379


----------------------------------------------------------------------------
Starting performance test of: PostgreSqlDocumentStore
----------------------------------------------------------------------------
Executing tests...
Creating 10000 small items in db...                         00:00:33.6252503
Fetching 10000 small items from db one by one...            00:00:09.0349484
Fetching 10000 small items from db at once...               00:00:00.0564641
Updating 10000 small items in db one by one...              00:00:32.9412380
Deleting 10000 small items from db one by one...            00:00:32.3691848
Creating 10 large items in db...                            00:00:32.4661386
Updating 10 large items in db one by one...                 00:00:00.0890408
Deleting 10 large items from db one by one...               00:00:00.0594444
Total execution time...                                     00:01:51.3178134
```

