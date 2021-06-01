/* Note: Connect to "dm-nr-oltp" database */

delete from api.[scale_out_replica];
insert into api.[scale_out_replica] values
('dm-nr-oltp-ro-01', 0),
('dm-nr-oltp-ro-02', 0)
;
