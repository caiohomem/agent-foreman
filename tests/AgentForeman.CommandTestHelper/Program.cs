var mode = args.Length > 0 ? args[0] : string.Empty;
var values = args.Skip(1).ToArray();

switch (mode)
{
    case "stdout":
        Console.Out.WriteLine(values.FirstOrDefault() ?? string.Empty);
        return 0;

    case "stderr":
        Console.Error.WriteLine(values.FirstOrDefault() ?? string.Empty);
        return 0;

    case "fail":
        return int.TryParse(values.FirstOrDefault(), out var exitCode) ? exitCode : 1;

    case "cwd":
        Console.Out.WriteLine(Environment.CurrentDirectory);
        return 0;

    case "sleep":
        var milliseconds = int.TryParse(values.FirstOrDefault(), out var delay) ? delay : 1000;
        await Task.Delay(milliseconds);
        return 0;

    case "args":
        foreach (var value in values)
        {
            Console.Out.WriteLine($"ARG:{value}");
        }

        return 0;

    default:
        Console.Error.WriteLine($"Unknown helper mode: {mode}");
        return 2;
}
