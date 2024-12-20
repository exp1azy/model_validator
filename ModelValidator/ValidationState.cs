namespace ModelValidator
{
    /// <summary>
    /// Represents the state of a single property during validation, including its validity and any associated error message.
    /// </summary>
    public class ValidationState
    {
        /// <summary>
        /// Gets or sets a value indicating whether the property is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the error message if the property is invalid. Null if the property is valid.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the name of the validated property.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the type of the validated property.
        /// </summary>
        public string PropertyType { get; set; }

        /// <summary>
        /// Creates a successful validation state for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyType">The type of the property.</param>
        /// <returns>A <see cref="ValidationState"/> indicating success.</returns>
        public static ValidationState Success(string propertyName, string propertyType) => new()
        {
            IsValid = true,
            ErrorMessage = null,
            PropertyName = propertyName,
            PropertyType = propertyType
        };

        /// <summary>
        /// Creates a failed validation state for the specified property with an error message.
        /// </summary>
        /// <param name="errorMessage">The error message describing the validation failure.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyType">The type of the property.</param>
        /// <returns>A <see cref="ValidationState"/> indicating failure.</returns>
        public static ValidationState Failure(string errorMessage, string propertyName, string propertyType) => new()
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            PropertyName = propertyName,
            PropertyType = propertyType
        };
    }
}
