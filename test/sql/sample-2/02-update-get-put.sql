alter procedure [api].[put_shopping_cart]
@payload nvarchar(max)
as
set nocount on;

begin tran;
exec sp_getapplock @Resource='dummy', @LockMode='Exclusive';
waitfor delay '00:00:00.100';

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
;

exec sp_releaseapplock @Resource='dummy';
commit tran;
go

alter procedure [api].[get_shopping_cart]
@id bigint
as
set nocount on;

begin tran;
exec sp_getapplock @Resource='dummy', @LockMode='Exclusive';
waitfor delay '00:00:00.100';

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

exec sp_releaseapplock @Resource='dummy';
commit tran;
go