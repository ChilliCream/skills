using System.Reflection;

var commandName = Assembly.GetExecutingAssembly()
    .GetCustomAttributes<AssemblyMetadataAttribute>()
    .FirstOrDefault(a => a.Key == "ToolCommandName")
    ?.Value;

return await Skills.Program.RunAsync(args, commandName);
