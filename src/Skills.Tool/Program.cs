using System.Reflection;

var commandName = Assembly.GetExecutingAssembly()
    .GetCustomAttributes<AssemblyMetadataAttribute>()
    .FirstOrDefault(a => a.Key == "ToolCommandName")
    ?.Value;

return await Skills.Cli.Program.RunAsync(args, commandName);
