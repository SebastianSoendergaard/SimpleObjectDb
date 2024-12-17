# SimpleObjectDb
Easy way to store data in a No SQL way

The solution consists of a generic interface that can be used by the application. The interface can be backed by different storage solutions. Currently the solution supports raw files and MS SQL Server as backing.

### Purpose
The main purpose with this is not to be used in large production systems but more during development. When developing, focus should be on the requirements and behavior not data. 
Following a clean architecture the data store can be stubbed by in-memory implementation but sometimes it is still nice to be able to save state between executions.

By using this you will have real persistency without any effort, this gives you time to fully implement your solution while postponing the desision of the actual data storage solution until the very last minute.
In some cases you may even find that using this is sufficient as the actual store solution. Especially in small application where high performance is not required.

### Usage 
For examples of usage see the file: Program.cs

To add to your project just copy the required files to your own project. This also allows for local changes if required.

For usage of the individual backing solutions go to the relevant folder for more info.
