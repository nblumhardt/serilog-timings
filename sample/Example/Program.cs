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

    using (var op = Operation.BeginWithTransformation("Adding {Count} successive integers", ts =>
           {
               var info = string.Format("{0:00} h :{1:00} m :{2:00} s :{3:00} mls", ts.Hours, ts.Minutes, ts.Seconds,
                   ts.Milliseconds / 10);
               return info;
           }, count))
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

