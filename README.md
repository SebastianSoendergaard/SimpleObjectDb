# SimpleDocumentStore
Easy way to store objects as documents without messing with any SQL or raw files.

The solution consists of a generic interface that can be used by the application. The interface can be backed by different storage solutions. Currently the solution supports raw files, MS SQL Server and PostgreSql as backing options. An in-memory implementation is also provided for testing purposes.

### Purpose
The main purpose with this is not to be used in large production systems but more during development or small simple applications. When developing, focus should be on the requirements and behavior not on data.
Following a clean architecture the data store can be stubbed by in-memory implementation but sometimes it is still nice to be able to save state between executions.

By using this you will have real persistency without any effort, this gives you time to fully implement your solution while postponing the desision of the actual data storage solution until it is actually needed.
In some cases you may even find that using this is a sufficient storeage solution. Especially in small application where the performance is not an issue.

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

For examples of usage see the solution: SimpleObjectDb

To run the eaxmples spin up docker with the required databases using docker compose:

```
\SimpleObjectDb>docker-compose up
```

#### Perfomance

The example application will execute some actions against the different database implementations and log the execution times. The actual results will of cause depend a lot on the machine running the application but the test will at least give some hints to how the performance is.

On my machine the output looks like this:

```
----------------------------------------------------------------------------
Starting performance test of: InMemoryDocumentStore
----------------------------------------------------------------------------
Creating test objects...                                    00:00:46.1738282
Executing tests...
Creating 10000 small items in db...                         00:00:00.0942609
Fetching 10000 small items from db one by one...            00:00:00.1121761
Updating 10000 small items in db one by one...              00:00:00.0961932
Fetching 10000 small items from db at once...               00:00:00.0975406
Deleting 10000 small items from db one by one...            00:00:00.0022015
Creating 1000 large items in db...                          00:00:00.3891205
Updating 1000 large items in db one by one...               00:00:31.8455183
Deleting 1000 large items from db one by one...             00:00:00.0008345
Total execution time...                                     00:00:32.6415006


----------------------------------------------------------------------------
Starting performance test of: FileDocumentStore
----------------------------------------------------------------------------
Creating test objects...                                    00:00:33.7969464
Executing tests...
Creating 10000 small items in db...                         00:00:04.3093183
Fetching 10000 small items from db one by one...            00:00:01.9770079
Updating 10000 small items in db one by one...              00:00:03.7104958
Fetching 10000 small items from db at once...               00:00:01.6707410
Deleting 10000 small items from db one by one...            00:00:01.0903333
Creating 1000 large items in db...                          00:00:02.2315231
Updating 1000 large items in db one by one...               00:00:37.9731740
Deleting 1000 large items from db one by one...             00:00:00.1649222
Total execution time...                                     00:00:52.0405606


----------------------------------------------------------------------------
Starting performance test of: SqlServerDocumentStore
----------------------------------------------------------------------------
Creating test objects...                                    00:00:34.6477269
Executing tests...
Creating 10000 small items in db...                         00:00:49.7486642
Fetching 10000 small items from db one by one...            00:00:36.5893511
Updating 10000 small items in db one by one...              00:02:04.1255227
Fetching 10000 small items from db at once...               00:00:00.0716380
Deleting 10000 small items from db one by one...            00:01:41.5766935
Creating 1000 large items in db...                          00:01:53.0871979
Updating 1000 large items in db one by one...               00:00:57.7546790
Deleting 1000 large items from db one by one...             00:00:06.5644556
Total execution time...                                     00:06:27.9449130


----------------------------------------------------------------------------
Starting performance test of: PostgreSqlDocumentStore
----------------------------------------------------------------------------
Creating test objects...                                    00:00:46.2382107
Executing tests...
Creating 10000 small items in db...                         00:00:34.4647823
Fetching 10000 small items from db one by one...            00:00:10.8677229
Updating 10000 small items in db one by one...              00:00:34.8558220
Fetching 10000 small items from db at once...               00:00:00.0959470
Deleting 10000 small items from db one by one...            00:00:35.2647267
Creating 1000 large items in db...                          00:00:48.1886589
Updating 1000 large items in db one by one...               00:01:05.2419559
Deleting 1000 large items from db one by one...             00:00:03.9881511
Total execution time...                                     00:03:17.7075068

```