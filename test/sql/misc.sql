/* Sample Code */

/* Add a new Named Replica */
/* In MASTER */
alter database [dm-nr-oltp]
add secondary on server [myserver]
with (secondary_type = Named, database_name = [dm-nr-oltp-ro-01])
go

/* In Local database */
exec [api].[get_available_scale_out_replicas];
go

insert into api.[scale_out_replica] values ('dm-nr-oltp-ro-01', 1)
go

select * from [api].[scale_out_replica] 
go

exec [api].[get_available_scale_out_replicas];
go

select * from sys.[dm_db_resource_stats]
go