select case(status) when 1 then 'active'
                   when 2 then 'inactive'
                   when 3 then 'pending'
                   else 'unknown' end as status_description
from orders