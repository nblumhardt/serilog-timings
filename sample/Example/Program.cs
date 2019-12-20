using System.Linq;
using Serilog;
using SerilogTimings;

namespace Example
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.LiterateConsole()
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();

            Log.Information("Hello, world!");

            var count = 11000;
            using (var op = Operation.Begin("Adding {Count} successive integers", count))
            {
                var sum = Enumerable.Range(0, count).Sum();
                Log.Information("This event is tagged with an operation id");

                op.Complete("Sum", sum);
            }

            Log.Information("Goodbye!");

            Log.CloseAndFlush();
        }
    }
}
