namespace FrenchExDev.Object.Net;

public abstract class AbstractObjectValidator<TClass, TMember> : IObjectValidator<TClass>
    where TClass : notnull
    where TMember : Enum
{
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

    protected abstract Task ValidateInternalAsync(TClass instance, ObjectValidation<TMember> dictionary, Dictionary<object, object> visited, CancellationToken cancellationToken = default);
}

public class ObjectValidation<TMember> : Dictionary<TMember, object>, IObjectValidation
    where TMember : Enum
{
    public bool IsValid => this.Count == 0;
}

public class FieldValidation<TMember, TObjectMemberValidation, TValue> : IObjectMemberValidation
    where TMember : Enum
    where TObjectMemberValidation : notnull
{
    public FieldValidation(TObjectMemberValidation validation, TMember member, TValue? value)
    {
        Validation = validation;
        Member = member;
        Value = value;
    }
    public TObjectMemberValidation Validation { get; }
    public TMember Member { get; }
    public TValue? Value { get; }
}

public abstract class AbstractObjectBuilder<TClass, TMember, TBuilder> : IObjectBuilder<TClass, TMember, TBuilder>
    where TClass : notnull, new()
    where TMember : Enum
    where TBuilder : IObjectBuilder<TClass, TMember, TBuilder>, new()
{
    // Utilisation de 'object' comme clé pour éviter le problème de contrainte générique.
    protected readonly Dictionary<object, object> VisitedInstances = new();

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

    protected abstract Task<TClass> BuildInternalAsync(TClass instance, Dictionary<object, object>? visited = null, CancellationToken cancellationToken = default);
}

public abstract class AbstractClass<TMember, TConcrete>
    where TMember : Enum
    where TConcrete : class
{
    public abstract TConcrete Set<T>(TMember member, T? value);
}