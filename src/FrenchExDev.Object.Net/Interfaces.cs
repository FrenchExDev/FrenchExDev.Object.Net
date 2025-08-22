namespace FrenchExDev.Object.Net;

public interface IObjectBuilder<TClass, TMember, TBuilder>
    where TClass : notnull
    where TMember : Enum
    where TBuilder : IObjectBuilder<TClass, TMember, TBuilder>, new()
{
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
    Task<IObjectValidation> ValidateAsync(TClass instance, Dictionary<object, object>? visited = null, CancellationToken cancellationToken = default);
}
