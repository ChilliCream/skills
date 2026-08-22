namespace Skills;

internal sealed class CommandServices(IServiceProvider services) : ICommandServices
{
    public T GetRequiredService<T>() where T : notnull
        => services.GetRequiredService<T>();
}
