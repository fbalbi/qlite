# storm
SimpleTinyORM - flexible .NET mapper to simplify SQL access
============================================================
I wanna take the opportunity to thank my old manager for firing me for my unwillingness to write hundreds of Oracle SQL statements (Proven Technology as he defined it) vs adopting Entity Framework.
This is pretty much when Storm was conceived and written. A simple C# class to access database backends and avoid manual SQL queries and data readers.
By using reflection Storm automatically generates SQL. By using Generics it also binds tables to objects allowing a more elegant data handling.

Storm was born as a wrapper for the Oracle Data Access and later SQL Server clients.
I recently switched to DbFactory in order to give the class an extra level of abstraction and not to be tied up to a specific driver.
I'm in fact messing around with MySQL.

Even though Storm is still under development I already have successfully used it in production environments.

I promise I will add examples and documentation!

Fed

