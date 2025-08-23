# `FrenchExDev.Object.Net`


[![NuGet](https://img.shields.io/nuget/v/FrenchExDev.Object.Net.svg)](https://www.nuget.org/packages/FrenchExDev.Object.Net/)
[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/FrenchExDev/FrenchExDev.Object.Net/BuildTestPublish.yaml?branch=main
)](https://github.com/FrenchExDev/FrenchExDev.Object.Net/actions/workflows/BuildTestPublish.yaml)
[![GitHub Branch Status#Main](https://img.shields.io/github/check-runs/FrenchExDev/FrenchExDev.Object.Net/main)](https://github.com/FrenchExDev/FrenchExDev.Object.Net/actions/workflows/BuildTestPublish.yaml)
[![GitHub Commit Activity](https://img.shields.io/github/commit-activity/w/FrenchExDev/FrenchExDev.Object.Net/main)](https://github.com/FrenchExDev/FrenchExDev.Object.Net/actions/workflows/BuildTestPublish.yaml)
[![NuGet](https://img.shields.io/nuget/dt/FrenchExDev.Object.Net.svg)](https://www.nuget.org/packages/FrenchExDev.Object.Net/)

A tiny & flexible library to build and validate .NET objects.

<!--TOC-->
- [Concepts](#concepts)
- [Object Building](#object-building)
- [Object Validation](#object-validation)
- [Example](#example)
  - [The Subject](#the-subject)
  - [The Subject' Builder](#the-subject-builder)
  - [The Subject' Validator](#the-subject-validator)
<!--/TOC-->

# Concepts

The library is built around two main concepts: object building and object validation. It provides abstract base classes that you can extend to create your own builders and validators for your specific objects.

# Object Building

The library provides a way to build objects using a fluent interface. You can define how to create an object and its properties in a clear and concise manner.

```csharp
public interface IObjectBuilder<TClass, TMember, TBuilder>
    where TClass : notnull
    where TMember : Enum
    where TBuilder : IObjectBuilder<TClass, TMember, TBuilder>, new()
{
    Task<TClass> BuildAsync(Dictionary<object, object>? visited = null, CancellationToken cancellationToken = default);
}
```

# Object Validation

The library also offers a way to validate objects. You can define validation code for your objects and check if they meet the criteria.

```csharp
public interface IObjectValidator<TClass>
{
    Task<IObjectValidation> ValidateAsync(TClass instance, CancellationToken cancellationToken = default);
}

public interface IObjectValidation
{

}

public interface IObjectMemberValidation
{

}

```

# Example

## The Subject

We use a simple class with a few properties to demonstrate the library's capabilities.

```csharp
internal class TestClass : AbstractClass<TestClass.Member, TestClass>
{
    public enum Member
    {
        Value,
        AnotherValue,
        NestedObject
    }

    public int? Value { get; set; }
    public string? AnotherValue { get; set; } = string.Empty;

    public TestClass? NestedObject { get; set; }

    public override TestClass Set<T>(Member member, T? value) where T : default
    {
        switch (member)
        {
            case Member.Value:
                Value = (int?)(object?)value;
                break;
            case Member.AnotherValue:
                AnotherValue = (string?)(object?)value;
                break;
            case Member.NestedObject:
                NestedObject = (TestClass?)(object?)value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(member), member, null);
        }
        return this;
    }
}
```

## The Subject' Builder

```csharp
internal class TestBuilder : AbstractObjectBuilder<TestClass, TestClass.Member, TestBuilder>
    {
        private int? _value;
        private string? _anotherValue;
        private TestBuilder? _nestedObject;

        public TestBuilder WithValue(int? value)
        {
            _value = value;
            return this;
        }

        public TestBuilder WithAnotherValue(string? anotherValue)
        {
            _anotherValue = anotherValue;
            return this;
        }

        public TestBuilder WithNestedObject(TestBuilder? nestedObject)
        {
            _nestedObject = nestedObject;
            return this;
        }

        public TestBuilder WithNestedObject(Action<TestBuilder> builder)
        {
            _nestedObject = new TestBuilder();
            builder(_nestedObject);
            return this;
        }

        protected override async Task<TestClass> BuildInternalAsync(TestClass instance, Dictionary<object, object>? visited = null, CancellationToken cancellationToken = default)
        {
            foreach (var member in Enum.GetValues<TestClass.Member>())
            {
                switch (member)
                {
                    case TestClass.Member.Value:
                        instance.Set(TestClass.Member.Value, _value);
                        break;
                    case TestClass.Member.AnotherValue:
                        instance.Set(TestClass.Member.AnotherValue, _anotherValue);
                        break;
                    case TestClass.Member.NestedObject:
                        if (_nestedObject != null)
                        {
                            instance.Set(TestClass.Member.NestedObject, await _nestedObject.BuildAsync(visited, cancellationToken));
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return instance;
        }
    }
```

## The Subject' Validator

```csharp
internal class TestValidator : AbstractObjectValidator<TestClass, TestClass.Member>
{
    protected override async Task ValidateInternalAsync(TestClass instance, ObjectValidation<TestClass.Member> dictionary, Dictionary<object, object> visited, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(dictionary);

        if (instance.NestedObject != null)
        {
            var nestedValidationResult = await ValidateAsync(instance.NestedObject, visited, cancellationToken);
            ArgumentNullException.ThrowIfNull(nestedValidationResult, nameof(nestedValidationResult));
            if (nestedValidationResult is ObjectValidation<TestClass.Member> nestedObjectValidationCast && !nestedObjectValidationCast.IsValid)
            {
                dictionary.Add(TestClass.Member.NestedObject, nestedValidationResult);
            }
        }

        if (instance.Value.HasValue && instance.Value.Value < 0)
        {
            dictionary.Add(TestClass.Member.Value, new FieldValidation<TestClass.Member, string, int?>("Value must be non-negative", TestClass.Member.Value, instance.Value));
        }

        if (instance.AnotherValue != null && instance.AnotherValue.Length < 5)
        {
            dictionary.Add(TestClass.Member.AnotherValue, new FieldValidation<TestClass.Member, string, string>("AnotherValue must be at least 5 characters long", TestClass.Member.AnotherValue, instance.AnotherValue));
        }
    }
}
```

## Putting It All Together

```csharp
[TestMethod]
public async Task CanBuildAndValidatComplexObjectWithCyclicReferences()
{
    var builder2 = new TestBuilder();
    var builder = new TestBuilder();

    builder.WithNestedObject(builder2).WithValue(1);
    builder2.WithValue(1).WithNestedObject(builder); // cyclic reference

    var instance = await builder.BuildAsync();
    var instance2 = await builder2.BuildAsync();

    var validator = new TestValidator();

    var validationResult = await validator.ValidateAsync(instance);
    validationResult.ShouldBeAssignableTo<ObjectValidation<TestClass.Member>>();

    var objectValidation = (ObjectValidation<TestClass.Member>)validationResult;

    objectValidation.ShouldNotBeNull();
    objectValidation.IsValid.ShouldBeTrue();
}
```

This example demonstrates how to use the `FrenchExDev.Object.Net` library to build and validate a simple object. 

You can extend the builder and validator classes to suit your specific needs and create more complex objects and validation rules.