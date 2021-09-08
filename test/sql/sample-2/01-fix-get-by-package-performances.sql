alter table dbo.[shopping_cart]
add package_id as cast(json_value(item_details, '$.package.id') as int)
go

create nonclustered index ix1 on dbo.[shopping_cart] (package_id)
go