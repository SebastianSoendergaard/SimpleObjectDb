The SimpleSqlServerObjectDb implementation is backed by a SQL Server. 

Run docker compose to start a local instance of SQL Server

`...\SimpleObjectDb\db\sqlserver>docker-compose up`

The actual database and tables needed can be created by the static mehods on SimpleSqlServerObjectDb or they can be created manually through a SQL script. For SQL script you can extract the required layout from the static methods on SimpleSqlServerObjectDb.
