/* Sample Code */

/* Add a new Named Replica */
/* In MASTER */
alter database [nroltp]
add secondary on server [myserver]
with (secondary_type = Named, database_name = [nroltp-ro01])
go

alter database [nroltp]
add secondary on server [myserver]
with (secondary_type = Named, database_name = [nroltp-ro02])
go

/* In "nroltp" database */
/* Notify the application that it can use the two created named replicas */
insert into api.[scale_out_replica] values ('nroltp-ro01', 1)
insert into api.[scale_out_replica] values ('nroltp-ro02', 1)
go

select * from [api].[scale_out_replica] 
go

exec [api].[get_available_scale_out_replicas];
go
