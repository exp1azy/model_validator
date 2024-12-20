namespace ModelValidator
{
    /// <summary>
    /// Represents the result of a validation process, including overall validity and individual property states.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the validation was successful for all properties.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the total number of properties validated.
        /// </summary>
        public int PropertyCount { get; set; }

        /// <summary>
        /// Gets or sets the collection of validation states for individual properties.
        /// </summary>
        public List<ValidationState> PropertyStates { get; set; }
    }
}
