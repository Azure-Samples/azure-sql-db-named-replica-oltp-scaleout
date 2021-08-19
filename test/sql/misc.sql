/* In MASTER */
alter database [dm-hs-test]
add secondary on server [myserver]
with (secondary_type = Named, database_name = [dm-hs-test-ea-ro-02])
go

/* In Local database */
select top (100) * from dbo.shopping_cart order by [cart_id] desc

select count(*)  from dbo.shopping_cart 

select top(10) json_value(item_details, '$.package.id'), * from dbo.shopping_cart where json_value(item_details, '$.package.id') is not null

alter sequence dbo.[cart_id_generator]  restart with 1

--truncate table dbo.shopping_cart

exec api.get_shopping_cart 398
exec [api].[get_shopping_cart_by_package] 1666

exec [api].[get_available_scale_out_replicas;
go

update [api].[scale_out_replica] set [enabled] = 0

select * from [api].[scale_out_replica] 

select * from sys.[dm_db_resource_stats]
