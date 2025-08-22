namespace FrenchExDev.Object.Net;

public abstract class AbstractObjectValidator<TClass, TMember> : IObjectValidator<TClass>
    where TClass : notnull
    where TMember : Enum
{
    private readonly Dictionary<object, ObjectValidation<TMember>> VisitedInstances = new();

    public async Task<IObjectValidation> ValidateAsync(TClass instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (VisitedInstances.TryGetValue(instance, out var existingValidation))
        {
            return existingValidation;
        }

        var dictionary = new ObjectValidation<TMember>();
        VisitedInstances.Add(instance, dictionary);
        await ValidateInternalAsync(instance, dictionary, cancellationToken);
        return dictionary;
    }

    protected abstract Task ValidateInternalAsync(TClass instance, ObjectValidation<TMember> dictionary, CancellationToken cancellationToken = default);
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

    public abstract TBuilder With<T>(TMember member, T? value);

    public abstract TBuilder With<TWithClass, TWithBuilder>(TMember member, Action<TWithBuilder> valueFactory)
        where TWithClass : notnull, new()
        where TWithBuilder : notnull, IObjectBuilder<TWithClass, TMember, TWithBuilder>, new();

    public abstract Task<TBuilder> WithAsync<TWithClass, TWithBuilder>(TMember member, Func<TWithBuilder, CancellationToken, Task> asyncValueFactory, CancellationToken cancellationToken = default)
        where TWithClass : notnull, new()
        where TWithBuilder : notnull, IObjectBuilder<TWithClass, TMember, TWithBuilder>, new();

    protected abstract Task<TClass> BuildInternalAsync(TClass instance, Dictionary<object, object>? visited = null, CancellationToken cancellationToken = default);
}

public abstract class AbstractClass<TMember, TConcrete>
    where TMember : Enum
    where TConcrete : class
{
    public abstract TConcrete Set<T>(TMember member, T? value);
}