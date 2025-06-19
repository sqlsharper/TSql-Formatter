// See https://aka.ms/new-console-template for more information

using CnSharp.Sql;

Console.WriteLine("T-SQL Formatting Demo");
Console.WriteLine("=====================================");
var formatter = new TSqlFormatter();
var directory = Path.Combine(AppContext.BaseDirectory, "Samples");
var files = Directory.GetFiles(directory, "*.sql").OrderBy(x => x);
foreach (var file in files)
{
    Console.WriteLine($"{Path.GetFileName(file)}");
    Console.WriteLine("-------------------------------------");
    var sql = File.ReadAllText(file);
    Console.WriteLine("---------------Original----------------");
    Console.WriteLine(sql);
    var formattedSql = formatter.Format(sql);
    Console.WriteLine("----------------Formatted--------------");
    Console.WriteLine(formattedSql);
    Console.WriteLine("=====================================");
}