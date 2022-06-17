using Serilog;
using SerilogTimings;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

try
{
    Log.Information("Hello, world!");

    const int count = 10000;
    using (var op = Operation.Begin("Adding {Count} successive integers", count))
    {
        var sum = Enumerable.Range(0, count).Sum();
        Log.Information("This event is tagged with an operation id");

        op.Complete("Sum", sum);
    }

    Log.Information("Goodbye!");
    return 0;
}
catch (Exception e)
{
    Log.Error(e, "Unhandled exception");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

