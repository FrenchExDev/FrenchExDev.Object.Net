namespace FrenchExDev.Object.Net;

/// <summary>
/// Abstract base class for validating objects of type <typeparamref name="TClass"/> with an enum <typeparamref name="TMember"/> storing members for validation.
/// </summary>
/// <typeparam name="TClass"></typeparam>
/// <typeparam name="TMember"></typeparam>
public abstract class AbstractObjectValidator<TClass, TMember> : IObjectValidator<TClass>
    where TClass : notnull
    where TMember : Enum
{
    /// <summary>
    /// Validate the given instance of <typeparamref name="TClass"/>.
    /// Avoids infinite loops by tracking visited instances.
    /// Provides a cancellation token to support cancellation of the validation process.
    /// </summary>
    /// <param name="instance">The instance of <typeparamref name="TClass"/> to validate.</param>
    /// <param name="visited">A dictionary tracking visited instances to avoid infinite loops.</param>
    /// <param name="cancellationToken">A cancellation token to support cancellation of the validation process.</param>
    /// <returns>A task representing the asynchronous validation operation.</returns>
    public async Task<IObjectValidation> ValidateAsync(TClass instance, Dictionary<object, object>? visited = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        visited ??= new();

        if (visited.TryGetValue(instance, out var existingValidation) && existingValidation is ObjectValidation<TMember> existing)
        {
            return existing;
        }

        var dictionary = new ObjectValidation<TMember>();

        visited.Add(instance, dictionary);

        await ValidateInternalAsync(instance, dictionary, visited, cancellationToken);

        return dictionary;
    }

    /// <summary>
    /// Abstract method to be implemented by derived classes to perform the actual validation logic.
    /// </summary>
    /// <param name="instance">The instance of <typeparamref name="TClass"/> to validate.</param>
    /// <param name="dictionary">The dictionary to store validation results.</param>
    /// <param name="visited">A dictionary tracking visited instances to avoid infinite loops.</param>
    /// <param name="cancellationToken">A cancellation token to support cancellation of the validation process.</param>
    /// <returns>A task representing the asynchronous validation operation.</returns>
    protected abstract Task ValidateInternalAsync(TClass instance, ObjectValidation<TMember> dictionary, Dictionary<object, object> visited, CancellationToken cancellationToken = default);
}

/// <summary>
/// Concrete implementation of <see cref="IObjectValidation"/> using a dictionary to store validation results.
/// </summary>
/// <typeparam name="TMember">Enum type with values representing the members subject to validation.</typeparam>
public class ObjectValidation<TMember> : Dictionary<TMember, object>, IObjectValidation
    where TMember : Enum
{
    /// <summary>
    /// Gets a value indicating whether the current state is valid.
    /// If the dictionary is empty, it indicates that there are no validation errors.
    /// </summary>
    public bool IsValid => this.Count == 0;
}

/// <summary>
/// Represents a validation operation for a specific field of an object, associating the field with its validation logic
/// and value.
/// </summary>
/// <typeparam name="TMember">The type of the field identifier, typically an enumeration representing the fields of the object.</typeparam>
/// <typeparam name="TObjectMemberValidation">The type of the validation. This type must be non-nullable. Can be a string or an interface for example.</typeparam>
/// <typeparam name="TValue">The type of the value being validated for the specified field.</typeparam>
public class FieldValidation<TMember, TObjectMemberValidation, TValue> : IObjectMemberValidation
    where TMember : Enum
    where TObjectMemberValidation : notnull
{

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldValidation{TObjectMemberValidation, TMember, TValue}"/> class
    /// with the specified member, and value.
    /// </summary>
    /// <param name="validation">Validation object</param>
    /// <param name="member">The member being validated</param>
    /// <param name="value">The value of the member</param>
    public FieldValidation(TObjectMemberValidation validation, TMember member, TValue? value)
    {
        Validation = validation;
        Member = member;
        Value = value;
    }

    /// <summary>
    /// Validation specific object
    /// </summary>
    public TObjectMemberValidation Validation { get; }

    /// <summary>
    /// Member being validated
    /// </summary>
    public TMember Member { get; }

    /// <summary>
    /// Gets the value associated with the current instance.
    /// </summary>
    public TValue? Value { get; }
}

/// <summary>
/// Provides a base class for building objects of type <typeparamref name="TClass"/> using a fluent interface.
/// </summary>
/// <remarks>This abstract class provides a framework for creating complex objects in a controlled and reusable
/// manner. It supports tracking of visited instances to handle circular references during the build process. Derived
/// classes must implement the <see cref="BuildInternalAsync"/> method to define the specific logic for building the
/// object.</remarks>
/// <typeparam name="TClass">The type of the object being built. Must be a non-nullable reference type with a parameterless constructor.</typeparam>
/// <typeparam name="TMember">An enumeration type representing the members or properties of the object being built.</typeparam>
/// <typeparam name="TBuilder">The type of the builder implementing this interface. Must implement <see cref="IObjectBuilder{TClass, TMember,
/// TBuilder}"/>.</typeparam>
public abstract class AbstractObjectBuilder<TClass, TMember, TBuilder> : IObjectBuilder<TClass, TMember, TBuilder>
    where TClass : notnull, new()
    where TMember : Enum
    where TBuilder : IObjectBuilder<TClass, TMember, TBuilder>, new()
{
    /// <summary>
    /// Holds references to already visited instances to prevent infinite loops during the build process.
    /// </summary>
    protected readonly Dictionary<object, object> VisitedInstances = new();

    /// <summary>
    /// Asynchronously builds an instance of the specified type, initializing it with the required data.
    /// </summary>
    /// <remarks>This method ensures that circular references are handled by using the <paramref
    /// name="visited"/> dictionary. If the current object has already been processed, the previously created instance
    /// is returned.</remarks>
    /// <param name="visited">An optional dictionary used to track already visited objects during the build process to prevent circular
    /// references. If not provided, a new dictionary will be created.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will be canceled if the token is triggered.</param>
    /// <returns>An instance of type <typeparamref name="TClass"/> that has been initialized with the required data.</returns>
    public async Task<TClass> BuildAsync(Dictionary<object, object>? visited = null, CancellationToken cancellationToken = default)
    {
        visited ??= new();

        if (visited.TryGetValue(this, out var existing))
        {
            return (TClass)existing;
        }

        var instance = new TClass();

        visited[this] = instance;

        await BuildInternalAsync(instance, visited, cancellationToken).ConfigureAwait(false);

        return instance;
    }

    /// <summary>
    /// Asynchronously builds and initializes an instance of the specified type.
    /// </summary>
    /// <remarks>This method is intended to be implemented by derived classes to define the specific logic for
    /// building and initializing an instance of the specified type. The implementation should handle any necessary
    /// setup or configuration for the instance.</remarks>
    /// <param name="instance">The instance to be built and initialized. This parameter must not be <see langword="null"/>.</param>
    /// <param name="visited">An optional dictionary used to track visited objects during the build process to prevent circular references. If
    /// <see langword="null"/>, a new dictionary will be created internally.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the fully built and initialized
    /// instance.</returns>
    protected abstract Task<TClass> BuildInternalAsync(TClass instance, Dictionary<object, object>? visited = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an abstract base class for managing members of type <typeparamref name="TMember"/>  and associating them
/// with values of type <typeparamref name="TConcrete"/>.
/// </summary>
/// <remarks>This class provides a mechanism for setting values associated with specific members of the 
/// <typeparamref name="TMember"/> enumeration. Derived classes must implement the <see cref="Set{T}"/>  method to
/// define the behavior for associating values with members.</remarks>
/// <typeparam name="TMember">The type of the member, constrained to an enumeration.</typeparam>
/// <typeparam name="TConcrete">The type of the associated value, constrained to a reference type.</typeparam>
public abstract class AbstractClass<TMember, TConcrete>
    where TMember : Enum
    where TConcrete : class
{
    /// <summary>
    /// Sets the specified member to the given value and returns the current instance for method chaining.
    /// </summary>
    /// <typeparam name="T">The type of the value to set.</typeparam>
    /// <param name="member">The member to be set. This parameter cannot be null.</param>
    /// <param name="value">The value to assign to the specified member. Can be null if the member supports nullable values.</param>
    /// <returns>The current instance of type <typeparamref name="TConcrete"/> to allow for method chaining.</returns>
    public abstract TConcrete Set<T>(TMember member, T? value);
}