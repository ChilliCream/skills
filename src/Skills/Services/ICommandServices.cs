namespace Skills;

internal interface ICommandServices
{
    T GetRequiredService<T>() where T : notnull;
}
