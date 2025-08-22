namespace FrenchExDev.Object.Net;

public interface IObjectBuilder<TClass>
    where TClass : notnull
{
    TClass Build();
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
