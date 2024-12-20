using System.Linq.Expressions;

namespace ModelValidator
{
    /// <summary>
    /// Abstract base class for validating models.
    /// Provides mechanisms for defining validation rules and validating model properties.
    /// </summary>
    public abstract class ModelValidator
    {
        private readonly List<ValidationRule> _rules = [];

        /// <summary>
        /// Abstract method for adding validation rules to the model.
        /// Must be implemented in derived classes to define custom rules for validation.
        /// </summary>
        public abstract void AddRules();

        /// <summary>
        /// Defines a validation rule for a specific property of the model.
        /// </summary>
        /// <param name="expression">An expression that specifies the property to validate.</param>
        /// <returns>
        /// A <see cref="ValidationRuleBuilder{TProperty}"/> that allows configuring the validation rule for the specified property.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="expression"/> is null.</exception>
        public ValidationRuleBuilder<object> RuleFor(Expression<Func<object>> expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

            var compiled = expression.Compile();
            var property = compiled();

            var rule = new ValidationRule
            {
                PropertyType = property.GetType().ToString(),
                PropertyName = GetPropertyName(expression),
                PropertyGetter = property
            };

            _rules.Add(rule);

            return new ValidationRuleBuilder<object>(rule);
        }

        /// <summary>
        /// Validates the model based on the defined validation rules.
        /// </summary>
        /// <typeparam name="TModel">The type of the model being validated, inheriting from <see cref="ModelValidator"/>.</typeparam>
        /// <returns>
        /// A <see cref="ValidationResult"/> containing the validation outcomes for each property and the overall validation state.
        /// </returns>
        public ValidationResult Validate<TModel>() where TModel : ModelValidator
        {
            var result = new ValidationResult
            {
                PropertyCount = _rules.Count,
                PropertyStates = []
            };

            foreach (var rule in _rules)
                result.PropertyStates.Add(rule.Validate<TModel>());

            if (result.PropertyStates.All(s => s.IsValid))
                result.IsValid = true;

            return result;
        }

        /// <summary>
        /// Retrieves the name of a property from the provided expression.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="expression">An expression representing the property.</param>
        /// <returns>The name of the property, or "UnknownPropertyName" if it cannot be determined.</returns>
        private static string GetPropertyName<TProperty>(Expression<Func<TProperty>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
                return memberExpression.Member.Name;

            if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression member)
                return member.Member.Name;

            return "UnknownPropertyName";
        }
    }

}
