namespace FrenchExDev.Object.Net;

/// <summary>
/// Interface for building objects of type <typeparamref name="TClass"/> with members defined by <typeparamref name="TMember"/> enum.
/// </summary>
/// <typeparam name="TClass"></typeparam>
/// <typeparam name="TMember"></typeparam>
/// <typeparam name="TBuilder"></typeparam>
public interface IObjectBuilder<TClass, TMember, TBuilder>
    where TClass : notnull
    where TMember : Enum
    where TBuilder : IObjectBuilder<TClass, TMember, TBuilder>, new()
{
    /// <summary>
    /// Asynchronously builds and returns an instance of <typeparamref name="TClass"/>.
    /// </summary>
    /// <param name="visited">An optional dictionary used to track objects that have already been processed during the build operation.  This
    /// can be used to prevent circular references or redundant processing. If not provided, a new dictionary will be
    /// created internally.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the constructed instance of
    /// <typeparamref name="TClass"/>.</returns>
    Task<TClass> BuildAsync(Dictionary<object, object>? visited = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Marker interface for object validation results.
/// </summary>
public interface IObjectValidation
{

}

/// <summary>
/// Marker interface for object member validation results.
/// </summary>
public interface IObjectMemberValidation
{

}

/// <summary>
/// Defines a mechanism for validating an object of type <typeparamref name="TClass"/>.
/// </summary>
/// <typeparam name="TClass">The type of the object to validate.</typeparam>
public interface IObjectValidator<TClass>
{
    /// <summary>
    /// Asynchronously validates the specified instance and returns the validation results.
    /// </summary>
    /// <remarks>This method performs a deep validation of the provided instance, including any nested
    /// objects,  and supports tracking of already visited objects to handle circular references. The validation 
    /// results include information about any validation errors or warnings encountered.</remarks>
    /// <param name="instance">The instance of type <typeparamref name="TClass"/> to validate. Cannot be <see langword="null"/>.</param>
    /// <param name="visited">An optional dictionary used to track objects that have already been visited during validation. This helps
    /// prevent circular references in complex object graphs.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will be canceled if the token is triggered.</param>
    /// <returns>A task that represents the asynchronous validation operation. The task result contains an  <see
    /// cref="IObjectValidation"/> object with the results of the validation.</returns>
    Task<IObjectValidation> ValidateAsync(TClass instance, Dictionary<object, object>? visited = null, CancellationToken cancellationToken = default);
}
