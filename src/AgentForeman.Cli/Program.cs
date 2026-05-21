using AgentForeman.Cli;

var result = CliApplication.Execute(args);

if (!string.IsNullOrEmpty(result.Output))
{
    Console.Out.Write(result.Output);
}

if (!string.IsNullOrEmpty(result.Error))
{
    Console.Error.Write(result.Error);
}

return result.ExitCode;
