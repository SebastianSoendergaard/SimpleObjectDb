The SimplePostgreSqlObjectDb implementation is backed by a PostgreSQL. 

Run docker compose to start a local instance of PostgreSQL

`...\SimpleObjectDb\db\postgresql>docker-compose up`

The actual database and tables needed can be created by the static mehods on SimplePostgreSqlObjectDb or they can be created manually through a SQL script. For SQL script you can extract the required layout from the static methods on SimplePostgreSqlObjectDb.
