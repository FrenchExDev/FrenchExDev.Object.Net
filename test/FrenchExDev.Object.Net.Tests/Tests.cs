using Shouldly;

namespace FrenchExDev.Object.Net.Tests;

[TestClass]
public sealed class Tests
{
    internal class TestClass
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

        public TestClass Set(int? value, string? anotherValue, TestClass? nestedObject)
        {
            Value = value;
            AnotherValue = anotherValue;
            NestedObject = nestedObject;
            return this;
        }
    }

    internal class TestBuilder : IObjectBuilder<TestClass>
    {
        private readonly Dictionary<IObjectBuilder<object>, object> VisitedInstances = new();

        private int? _value;
        private string? _anotherValue;
        private TestBuilder? _nestedObject;

        public TestBuilder WithValue(int value)
        {
            _value = value;
            return this;
        }

        public TestBuilder WithAnotherValue(string anotherValue)
        {
            _anotherValue = anotherValue;
            return this;
        }

        public TestBuilder WithNestedObject(Action<TestBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            _nestedObject = new TestBuilder();
            configure(_nestedObject);
            return this;
        }

        public TestBuilder WithNestedObject(TestBuilder other)
        {
            ArgumentNullException.ThrowIfNull(other);
            _nestedObject = other;
            return this;
        }

        public TestClass Build(Dictionary<object, object>? visited = null)
        {
            visited ??= new();

            if (visited.TryGetValue(this, out var existing))
            {
                return (TestClass)existing;
            }

            var instance = new TestClass();

            visited[this] = instance;

            instance.Set(_value, _anotherValue, _nestedObject?.Build(visited));

            return instance;
        }
    }

    internal class TestValidator : AbstractObjectValidator<TestClass, TestClass.Member>
    {
        protected override async Task ValidateInternalAsync(TestClass instance, ObjectValidation<TestClass.Member> dictionary, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(dictionary);

            if (instance.NestedObject != null)
            {
                var nestedValidationResult = await ValidateAsync(instance.NestedObject, cancellationToken);
                ArgumentNullException.ThrowIfNull(nestedValidationResult, nameof(nestedValidationResult));
                dictionary.Add(TestClass.Member.NestedObject, nestedValidationResult);
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

    [TestMethod]
    public async Task CanBuildAndValidateSimpleObject()
    {
        var builder = new TestBuilder();

        var instance = builder.WithValue(5).Build();

        var validator = new TestValidator();
        var result = await validator.ValidateAsync(instance);
        result.ShouldBeAssignableTo<ObjectValidation<TestClass.Member>>();

        var objectValidation = (ObjectValidation<TestClass.Member>)result;
        objectValidation.ShouldNotBeNull();
        objectValidation.IsValid.ShouldBeTrue();
    }

    [TestMethod]
    public async Task CanBuildAndValidatComplexObject()
    {
        var builder = new TestBuilder();

        var instance = builder
            .WithValue(5)
            .WithAnotherValue("foo1")
            .WithNestedObject((b) => b
                .WithValue(-1)
                .WithAnotherValue("foo2")
                .WithNestedObject((b) => b
                    .WithAnotherValue("foo3")
                    .WithValue(-2)
                    .WithNestedObject(builder))).Build();

        var validator = new TestValidator();

        var validationResult = await validator.ValidateAsync(instance);
        validationResult.ShouldBeAssignableTo<ObjectValidation<TestClass.Member>>();

        var objectValidation = (ObjectValidation<TestClass.Member>)validationResult;
        objectValidation.ShouldNotBeNull();
        objectValidation.IsValid.ShouldBeFalse();

        objectValidation[TestClass.Member.AnotherValue].ShouldBeAssignableTo<FieldValidation<TestClass.Member, string, string>>();
    }

    [TestMethod]
    public async Task CanBuildAndValidatComplexObjectWithCyclicReferences()
    {
        var builder = new TestBuilder();

        var instance = builder.Build();

        var builder2 = new TestBuilder();
        var instance2 = builder2.Build();

        instance.NestedObject = instance2;
        instance.NestedObject.Value = -1;

        instance2.NestedObject = instance; // Create cyclic reference

        var validator = new TestValidator();

        var validationResult = await validator.ValidateAsync(instance);
        validationResult.ShouldBeAssignableTo<ObjectValidation<TestClass.Member>>();

        var objectValidation = (ObjectValidation<TestClass.Member>)validationResult;

        objectValidation.ShouldNotBeNull();
        objectValidation.IsValid.ShouldBeFalse();
    }
}
