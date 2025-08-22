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
    }

    internal class TestBuilder : IObjectBuilder<TestClass>
    {
        private readonly TestClass _instance = new();
        public TestClass Build()
        {
            return _instance;
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
                dictionary.Add(TestClass.Member.Value, new FieldValidation<TestClass.Member, string>("Value must be non-negative", TestClass.Member.Value));
            }
        }
    }

    [TestMethod]
    public async Task CanBuildAndValidateSimpleObject()
    {
        var builder = new TestBuilder();
        var instance = builder.Build();
        instance.Value = 5;

        var validator = new TestValidator();
        var result = await validator.ValidateAsync(instance);
        result.ShouldBeAssignableTo<ObjectValidation<TestClass.Member>>();

        var objectValidation = (ObjectValidation<TestClass.Member>)result;

        objectValidation.IsValid.ShouldBeTrue();
    }

    [TestMethod]
    public async Task CanBuildAndValidatComplexObject()
    {
        var builder = new TestBuilder();

        var instance = builder.Build();

        var builder2 = new TestBuilder();
        var instance2 = builder2.Build();

        instance.NestedObject = instance2;
        instance.NestedObject.Value = -1;

        var validator = new TestValidator();

        var validationResult = await validator.ValidateAsync(instance);
        validationResult.ShouldBeAssignableTo<ObjectValidation<TestClass.Member>>();

        var objectValidation = (ObjectValidation<TestClass.Member>)validationResult;

        objectValidation.IsValid.ShouldBeFalse();
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

        objectValidation.IsValid.ShouldBeFalse();
    }
}
