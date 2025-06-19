select * from (
  select a, b, c from t
) as subquery
where a > 10;