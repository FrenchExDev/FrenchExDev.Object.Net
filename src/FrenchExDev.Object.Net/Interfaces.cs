namespace FrenchExDev.Object.Net;

public interface IObjectBuilder<TClass>
    where TClass : notnull
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
    Task<IObjectValidation> ValidateAsync(TClass instance, CancellationToken cancellationToken = default);
}
