namespace FrenchExDev.Object.Net;

public interface IObjectBuilder<TClass, TMember, TBuilder>
    where TClass : notnull
    where TMember : Enum
    where TBuilder : IObjectBuilder<TClass, TMember, TBuilder>, new()
{
    public TBuilder With<T>(TMember member, T? value);

    public TBuilder With<TWithClass, TWithBuilder>(TMember member, Action<TWithBuilder> valueFactory)
        where TWithClass : notnull, new()
        where TWithBuilder : notnull, IObjectBuilder<TWithClass, TMember, TWithBuilder>, new();
    public Task<TBuilder> WithAsync<TWithClass, TWithBuilder>(TMember member, Func<TWithBuilder, CancellationToken, Task> asyncValueFactory, CancellationToken cancellationToken = default)
        where TWithClass : notnull, new()
        where TWithBuilder : notnull, IObjectBuilder<TWithClass, TMember, TWithBuilder>, new();

    Task<TClass> BuildAsync(Dictionary<object, object>? visited = null, CancellationToken cancellationToken = default);
}

public interface IObjectValidation
{

}

public interface IObjectMemberValidation
{

}

public interface IObjectValidator<TClass>
{
    Task<IObjectValidation> ValidateAsync(TClass instance, CancellationToken cancellationToken = default);
}
