alter procedure [api].[get_shopping_cart]
@id bigint
as
set nocount on;

begin tran;
exec sp_getapplock @Resource='dummy', @LockMode='Exclusive';
waitfor delay '00:00:00.010';

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