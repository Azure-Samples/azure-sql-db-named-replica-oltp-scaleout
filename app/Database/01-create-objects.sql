/* Note: Connect to "nroltp" database */

if schema_id('api') is null begin
	exec('create schema api authorization dbo');
end
go

if (user_id('webuser') is null) begin
	create user webuser with password = 'WEB_US3r|Passw0rd!'
end
go

grant execute on schema::api to [webuser]
go

drop sequence if exists dbo.cart_id_generator;
create sequence dbo.cart_id_generator
as bigint start with 1 increment by 1;
go

drop table if exists api.scale_out_replica;
create table api.scale_out_replica
(
	[database_name] sysname not null unique,
	[enabled] bit not null default(0),
	[tag] sysname not null default('GenericRead') check ([tag] in ('GenericRead', 'Search', 'Reporting', 'SingletonLookup'))
)
go

create or alter procedure [api].[get_available_scale_out_replicas]
as
select
	[tag] as Tag, [database_name] as DatabaseName
from
	[api].[scale_out_replica]
where
	[enabled] = 1
order by
	Tag
go		

drop table if exists dbo.shopping_cart;
create table dbo.shopping_cart
(
	[row_id] int identity not null primary key nonclustered,
	[cart_id] bigint not null,
	[user_id] int not null,
	[item_id] int not null,
	[quantity] int not null,
	[price] decimal (10,4) not null,
	[item_details] nvarchar(max) not null check (isjson(item_details) = 1),
	[added_on] datetime2 not null
)
go

create clustered index ixc on dbo.shopping_cart([cart_id])
go

alter table dbo.[shopping_cart]
add package_id as cast(json_value(item_details, '$.package.id') as int)
go

create nonclustered index ix1 on dbo.[shopping_cart] (package_id)
go

create or alter procedure api.put_shopping_cart
@payload nvarchar(max)
as
set nocount on;

declare @cart_id bigint = next value for dbo.cart_id_generator;

insert into dbo.shopping_cart
	([cart_id], [user_id], [item_id], [quantity], [price], [item_details], [added_on])
select 
	@cart_id,
	c.[user_id], 
	i.[item_id],
	i.[quantity],
	i.[price],
	i.[item_details],
	sysdatetime()
from 
	openjson(@payload) with	(
		[user_id] int, 
		[items] nvarchar(max) as json
	) as c
cross apply
	openjson(c.[items]) with (
		[item_id] int '$.id',
		[quantity] int,
		[price] decimal(10,4),
		[item_details] nvarchar(max) '$.details' as json 
	) as i
go

create or alter procedure api.get_shopping_cart
@id bigint
as
set nocount on;
select ((
	select top (1)
		c.cart_id,
		c.[user_id],
		json_query((
			select
				item_id as 'id',
				quantity,
				price,
				json_query(item_details) as 'details'
			from
				dbo.shopping_cart as items
			where
				items.cart_id = c.cart_id
			and
				items.cart_id = @id
			for json path
		)) as items
	from 
		dbo.shopping_cart c
	where
		cart_id = @id
	for json auto, without_array_wrapper
)) as json_result;
go

create or alter procedure [api].[get_shopping_cart_by_package]
@id bigint
as
set nocount on;
select ((
	select top (1)
		c.cart_id,
		c.[user_id],
		json_query((
			select
				item_id as 'id',
				quantity,
				price,
				json_query(item_details) as 'details'
			from
				dbo.shopping_cart as items
			where
				items.cart_id = c.cart_id
			for json path
		)) as items
	from 
		dbo.shopping_cart c
	where
		cast(json_value(item_details, '$.package.id') as int) = @id
	for json auto
)) as json_result;
