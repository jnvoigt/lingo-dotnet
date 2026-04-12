using Lingo.Cli.Commands;
using System.CommandLine;
using System.Threading.Tasks;

namespace Lingo.Cli;

public static class Program
{
    public static async Task Main(params string[] args)
    {
        var rootCommand = new RootCommand("Lingo localization tool");
        rootCommand.Add(SyncCommand.GetCommand());
        await rootCommand.Parse(args).InvokeAsync();
    }
}