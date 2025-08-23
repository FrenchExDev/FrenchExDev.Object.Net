using Shouldly;
using static FrenchExDev.Object.Net.Tests.Tests.TestClass;

namespace FrenchExDev.Object.Net.Tests;

/// <summary>
/// Testing implementation of <see cref="AbstractObjectBuilder{TObject, TMember, TBuilder}"/> and <see cref="AbstractObjectValidator{TObject, TMember}"/>
/// </summary>
[TestClass]
public sealed class Tests
{
    /// <summary>
    /// A simple test class implementing <see cref="AbstractClass{TMember, TSelf}"/>
    /// </summary>
    internal class TestClass : AbstractClass<TestClass.Member, TestClass>
    {
        /// <summary>
        /// An enum representing the members of <see cref="TestClass"/>
        /// </summary>
        public enum Member
        {
            Value,
            AnotherValue,
            NestedObject
        }

        /// <summary>
        /// A simple integer value
        /// </summary>
        public int? Value { get; set; }

        /// <summary>
        /// Another simple value
        /// </summary>
        public string? AnotherValue { get; set; } = string.Empty;

        /// <summary>
        /// A nested object of the same type to test recursive validation
        /// </summary>
        public TestClass? NestedObject { get; set; }

        /// <summary>
        /// Set logic for <see cref="TestClass"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="member"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
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

    /// <summary>
    /// Builder implementation for <see cref="TestClass"/>
    /// </summary>
    internal class TestBuilder : AbstractObjectBuilder<TestClass, TestClass.Member, TestBuilder>
    {
        /// <summary>
        /// Holds the value for <see cref="TestClass.Value"/>
        /// </summary>
        private int? _value;

        /// <summary>
        /// Holds the value for <see cref="TestClass.AnotherValue"/>
        /// </summary>
        private string? _anotherValue;

        /// <summary>
        /// Holds the builder for <see cref="TestClass.NestedObject"/>
        /// </summary>
        private TestBuilder? _nestedObject;

        /// <summary>
        /// Provides a fluent way to set <see cref="TestClass.Value"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public TestBuilder WithValue(int? value)
        {
            _value = value;
            return this;
        }

        /// <summary>
        /// Provides a fluent way to set <see cref="TestClass.AnotherValue"/>
        /// </summary>
        /// <param name="anotherValue"></param>
        /// <returns></returns>
        public TestBuilder WithAnotherValue(string? anotherValue)
        {
            _anotherValue = anotherValue;
            return this;
        }

        /// <summary>
        /// Provides a fluent way to set <see cref="TestClass.NestedObject"/>
        /// </summary>
        /// <param name="nestedObject"></param>
        /// <returns></returns>
        public TestBuilder WithNestedObject(TestBuilder? nestedObject)
        {
            _nestedObject = nestedObject;
            return this;
        }

        /// <summary>
        /// Configures a nested <see cref="TestBuilder"/> instance using the specified builder action.
        /// </summary>
        /// <remarks>This method creates a new nested <see cref="TestBuilder"/> instance, applies the
        /// provided  configuration action to it, and retains the nested object for further use.</remarks>
        /// <param name="builder">An action that configures the nested <see cref="TestBuilder"/> instance.  The action receives the newly
        /// created <see cref="TestBuilder"/> as a parameter.</param>
        /// <returns>The current <see cref="TestBuilder"/> instance, allowing for method chaining.</returns>
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

    /// <summary>
    /// Provides validation logic for instances of <see cref="TestClass"/> and its members.
    /// </summary>
    /// <remarks>This class performs validation on <see cref="TestClass"/> objects, including nested objects
    /// and specific member values. It ensures that the validated object and its members meet the defined constraints,
    /// such as non-negative values or minimum string lengths.</remarks>
    internal class TestValidator : AbstractObjectValidator<TestClass, TestClass.Member>
    {
        /// <summary>
        /// This method is called internally by <see cref="AbstractObjectValidator{TObject, TMember}.ValidateAsync(TObject, Dictionary{object, object}, CancellationToken)"/>
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="dictionary"></param>
        /// <param name="visited"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task ValidateInternalAsync(TestClass instance, ObjectValidation<TestClass.Member> dictionary, Dictionary<object, object> visited, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(dictionary);
            ArgumentNullException.ThrowIfNull(visited);

            await ValidateInternalNestedObjectAsync(instance, dictionary, visited, cancellationToken);

            if (instance.Value.HasValue && instance.Value.Value < 0)
            {
                dictionary.Add(TestClass.Member.Value, new FieldValidation<TestClass.Member, string, int?>("Value must be non-negative", TestClass.Member.Value, instance.Value));
            }

            if (instance.AnotherValue != null && instance.AnotherValue.Length < 5)
            {
                dictionary.Add(TestClass.Member.AnotherValue, new FieldValidation<TestClass.Member, string, string>("AnotherValue must be at least 5 characters long", TestClass.Member.AnotherValue, instance.AnotherValue));
            }
        }

        private async Task ValidateInternalNestedObjectAsync(TestClass instance, ObjectValidation<Member> dictionary, Dictionary<object, object> visited, CancellationToken cancellationToken)
        {
            if (instance.NestedObject != null)
            {
                var nestedValidationResult = await ValidateAsync(instance.NestedObject, visited, cancellationToken);
                ArgumentNullException.ThrowIfNull(nestedValidationResult, nameof(nestedValidationResult));
                if (nestedValidationResult is ObjectValidation<TestClass.Member> nestedObjectValidationCast && !nestedObjectValidationCast.IsValid)
                {
                    dictionary.Add(TestClass.Member.NestedObject, nestedValidationResult);
                }
            }
        }
    }

    [TestMethod]
    public async Task CanBuildAndValidateSimpleObject()
    {
        var builder = new TestBuilder().WithValue(5);

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
            .WithValue(5)
            .WithAnotherValue("foo1")
            .WithNestedObject((b) => b
                .WithValue(-1)
                .WithAnotherValue("foo2")
                .WithNestedObject((b) => b
                    .WithAnotherValue("foo3")
                    .WithValue(-2)
                    .WithNestedObject(builder))).BuildAsync();

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
}
