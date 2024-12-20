namespace ModelValidator
{
    /// <summary>
    /// Represents a validation rule for a specific property, including its type, name, and associated validators.
    /// </summary>
    public class ValidationRule
    {
        private readonly List<Func<object?, ValidationState>> _validators = [];

        /// <summary>
        /// Gets or sets the type of the property being validated.
        /// </summary>
        public string PropertyType { get; set; }

        /// <summary>
        /// Gets or sets the name of the property being validated.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the getter for the property value being validated.
        /// </summary>
        public object? PropertyGetter { get; set; }

        /// <summary>
        /// Retrieves the first validator and applies it to the property value.
        /// </summary>
        /// <returns>A <see cref="ValidationState"/> indicating the result of the validation.</returns>
        public ValidationState GetValidator() => _validators.First()(PropertyGetter);

        /// <summary>
        /// Adds a validator function to the rule.
        /// </summary>
        /// <param name="validator">The validator function to add. It should return a <see cref="ValidationState"/>.</param>
        public void AddValidator(Func<object?, ValidationState> validator)
        {
            _validators.Add(validator);
        }

        /// <summary>
        /// Validates the property using all assigned validators. Stops at the first failed validation.
        /// </summary>
        /// <typeparam name="TModel">The type of the model being validated.</typeparam>
        /// <returns>
        /// A <see cref="ValidationState"/> indicating the result of the validation.
        /// If all validators pass, returns a successful validation state.
        /// </returns>
        public ValidationState Validate<TModel>()
        {
            var value = PropertyGetter;

            foreach (var validator in _validators)
            {
                var result = validator(value);

                if (!result.IsValid)
                    return result;
            }

            return ValidationState.Success(PropertyName, PropertyType);
        }
    }
}
