using Shouldly;

namespace FrenchExDev.Object.Net.Tests;

[TestClass]
public sealed class Tests
{
    public abstract class AbstractObjectValidator<TClass> : IObjectValidator<TClass>
        where TClass : notnull
    {
        private readonly Dictionary<object, ObjectValidation> VisitedInstances = new();

        public async Task<IObjectValidation> ValidateAsync(TClass instance, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            if (VisitedInstances.TryGetValue(instance, out var existingValidation))
            {
                return existingValidation;
            }

            var dictionary = new ObjectValidation();
            VisitedInstances.Add(instance, dictionary);
            await ValidateInternalAsync(instance, dictionary, cancellationToken);
            return dictionary;
        }

        protected abstract Task ValidateInternalAsync(TClass instance, ObjectValidation dictionary, CancellationToken cancellationToken = default);
    }


    public class ObjectValidation : Dictionary<string, object>, IObjectValidation
    {
        public bool IsValid => this.Count == 0;
    }

    public class FieldValidation<TObjectMemberValidation> : IObjectMemberValidation
         where TObjectMemberValidation : notnull
    {
        public FieldValidation(TObjectMemberValidation validation, string member)
        {
            Validation = validation;
            Member = member;
        }
        public TObjectMemberValidation Validation { get; }
        public string Member { get; }
    }

    internal class TestClass
    {
        public enum Members
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

    internal class TestValidator : AbstractObjectValidator<TestClass>
    {
        protected override async Task ValidateInternalAsync(TestClass instance, ObjectValidation dictionary, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(dictionary);

            if (instance.NestedObject != null)
            {
                var nestedValidationResult = await ValidateAsync(instance.NestedObject, cancellationToken);
                ArgumentNullException.ThrowIfNull(nestedValidationResult, nameof(nestedValidationResult));
                dictionary.Add(TestClass.Members.NestedObject.ToString(), nestedValidationResult);
            }

            if (instance.Value.HasValue && instance.Value.Value < 0)
            {
                dictionary.Add(TestClass.Members.Value.ToString(), new FieldValidation<string>("Value must be non-negative", TestClass.Members.Value.ToString()));
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
        result.ShouldBeAssignableTo<ObjectValidation>();

        var objectValidation = (ObjectValidation)result;

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
        validationResult.ShouldBeAssignableTo<ObjectValidation>();

        var objectValidation = (ObjectValidation)validationResult;

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
        validationResult.ShouldBeAssignableTo<ObjectValidation>();

        var objectValidation = (ObjectValidation)validationResult;

        objectValidation.IsValid.ShouldBeFalse();
    }
}
