using Shouldly;

namespace FrenchExDev.Object.Net.Tests;

[TestClass]
public sealed class Tests
{
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

    internal class TestBuilder : AbstractObjectBuilder<TestClass, TestClass.Member, TestBuilder>
    {
        private int? _value;
        private string? _anotherValue;
        private TestBuilder? _nestedObject;

        public override TestBuilder With<T>(TestClass.Member member, T? value) where T : default
        {
            switch (member)
            {
                case TestClass.Member.Value:
                    _value = (int?)(object?)value;
                    break;
                case TestClass.Member.AnotherValue:
                    _anotherValue = (string?)(object?)value;
                    break;
                case TestClass.Member.NestedObject:
                    _nestedObject = (TestBuilder?)(object?)value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(member), member, null);
            }
            return this;
        }

        public override TestBuilder With<TWithClass, TWithBuilder>(TestClass.Member member, Action<TWithBuilder> valueFactory)
        {
            switch (member)
            {
                case TestClass.Member.NestedObject:
                    var builder = new TWithBuilder();
                    valueFactory(builder);
                    _nestedObject = (TestBuilder?)(object?)builder;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(member), member, null);
            }

            return this;
        }

        public override async Task<TestBuilder> WithAsync<TWithClass, TWithBuilder>(TestClass.Member member, Func<TWithBuilder, CancellationToken, Task> asyncValueFactory, CancellationToken cancellationToken = default)
        {
            switch (member)
            {
                case TestClass.Member.NestedObject:
                    var builder = new TWithBuilder();
                    await asyncValueFactory(builder, cancellationToken);
                    _nestedObject = (TestBuilder?)(object?)builder;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(member), member, null);
            }

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
        var builder = new TestBuilder().With(TestClass.Member.Value, 5);

        var instance = await builder.BuildAsync();

        instance.ShouldBeAssignableTo<TestClass>();

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

        var instance = await builder
            .With(TestClass.Member.Value, 5)
            .With(TestClass.Member.AnotherValue, "foo1")
            .With<TestClass, TestBuilder>(TestClass.Member.NestedObject, valueFactory: (b) => b
                .With(TestClass.Member.Value, -1)
                .With(TestClass.Member.AnotherValue, "foo2")
                .With<TestClass, TestBuilder>(TestClass.Member.NestedObject, (b) => b
                    .With(TestClass.Member.AnotherValue, "foo3")
                    .With(TestClass.Member.Value, -2)
                    .With(TestClass.Member.NestedObject, builder))).BuildAsync();

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

        var instance = await builder.BuildAsync();

        var builder2 = new TestBuilder();
        var instance2 = await builder2.BuildAsync();

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
