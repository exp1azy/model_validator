using ModelValidator.Resources;
using System.Collections;
using System.Text.RegularExpressions;

namespace ModelValidator
{
    /// <summary>
    /// Provides a builder for configuring validation rules for a property of type <typeparamref name="TProperty"/>.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property to validate.</typeparam>
    /// <param name="rule">The associated validation rule.</param>
    public class ValidationRuleBuilder<TProperty>(ValidationRule rule)
    {
        private readonly ValidationRule _rule = rule;

        /// <summary>
        /// Adds a validation function to the rule and returns the builder for further configuration.
        /// </summary>
        /// <param name="validator">The validation function to add.</param>
        /// <returns>The current instance of <see cref="ValidationRuleBuilder{TProperty}"/>.</returns>
        private ValidationRuleBuilder<TProperty> ProcessValidation(Func<object?, ValidationState> validator)
        {
            _rule.AddValidator(validator);
            return this;
        }

        /// <summary>
        /// Validates the length of the property against a specified value using a comparison function.
        /// </summary>
        /// <param name="value">The property value to validate.</param>
        /// <param name="length">The length to compare against.</param>
        /// <param name="comparison">The comparison function.</param>
        /// <param name="errorMessage">The error message to return if validation fails.</param>
        /// <returns>A <see cref="ValidationState"/> indicating the result of the validation.</returns>
        private ValidationState ValidateLength(object? value, int length, Func<int, bool> comparison, string errorMessage)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(Error.LengthError);

            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            int actualLength = value switch
            {
                string stringValue => stringValue.Length,
                int intValue => intValue,
                double doubleValue => (int)doubleValue,
                float floatValue => (int)floatValue,
                decimal decimalValue => (int)decimalValue,
                long longValue => (int)longValue,
                short shortValue => shortValue,
                byte byteValue => byteValue,
                ICollection collectionValue => collectionValue.Count,
                _ => throw new ArgumentException(string.Format(Error.UnsupportedType, value.GetType().Name))
            };

            return !comparison(actualLength) ?
                ValidationState.Failure(string.Format(errorMessage, _rule.PropertyName, length), _rule.PropertyName, _rule.PropertyType) :
                ValidationState.Success(_rule.PropertyName, _rule.PropertyType);
        }

        /// <summary>
        /// Compares the value of the property against a boundary using a specified comparison function.
        /// </summary>
        /// <param name="value">The property value to validate.</param>
        /// <param name="comparison">The comparison function.</param>
        /// <param name="boundary">The boundary value to compare against.</param>
        /// <param name="errorMessage">The error message to return if validation fails.</param>
        /// <returns>A <see cref="ValidationState"/> indicating the result of the validation.</returns>
        private ValidationState CompareValue(object? value, Func<double, bool> comparison, int boundary, string errorMessage)
        {
            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            double? valueToCompare = value switch
            {
                int intValue => intValue,
                double doubleValue => doubleValue,
                float floatValue => floatValue,
                decimal decimalValue => (double)decimalValue,
                long longValue => longValue,
                short shortValue => shortValue,
                byte byteValue => byteValue,
                DateTime dateTimeValue => dateTimeValue.ToOADate(),
                DateOnly dateOnlyValue => dateOnlyValue.ToDateTime(TimeOnly.MinValue).ToOADate(),
                string strValue => strValue.Length,
                TimeSpan timeSpanValue => timeSpanValue.TotalMilliseconds,
                Enum enumValue => Convert.ToDouble(enumValue),
                Guid guidValue => BitConverter.ToInt64(guidValue.ToByteArray(), 0),
                _ => throw new ArgumentException(string.Format(Error.UnsupportedType, value.GetType().Name))
            };

            return valueToCompare.HasValue && !comparison(valueToCompare.Value) ?
                ValidationState.Failure(string.Format(errorMessage, _rule.PropertyName, boundary), _rule.PropertyName, _rule.PropertyType) :
                ValidationState.Success(_rule.PropertyName, _rule.PropertyType);
        }

        /// <summary>
        /// Validates the property as a date value, comparing it to a specified boundary using a comparison function.
        /// </summary>
        /// <param name="boundary">The boundary date for comparison.</param>
        /// <param name="comparison">The comparison function.</param>
        /// <param name="errorMessage">The error message to return if validation fails.</param>
        /// <returns>The current instance of <see cref="ValidationRuleBuilder{TProperty}"/>.</returns>
        private ValidationRuleBuilder<TProperty> ValidateDate(DateTime? boundary, Func<DateTime, DateTime, bool> comparison, string errorMessage) => ProcessValidation(value =>
        {
            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            var dateTimeValue = value switch
            {
                DateTime dt => dt,
                DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
                _ => throw new ArgumentException(string.Format(Error.UnsupportedType, value.GetType().Name))
            };

            return boundary.HasValue && !comparison(dateTimeValue, boundary.Value) ?
                ValidationState.Failure(string.Format(errorMessage, _rule.PropertyName, boundary), _rule.PropertyName, _rule.PropertyType) :
                ValidationState.Success(_rule.PropertyName, _rule.PropertyType);
        });

        /// <summary>
        /// Validates that the specified value falls within a specified range.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="min">The minimum allowable value.</param>
        /// <param name="max">The maximum allowable value.</param>
        /// <param name="inclusive">Indicates whether the range is inclusive or exclusive.</param>
        /// <returns>A <see cref="ValidationState"/> representing the result of the validation.</returns>
        /// <exception cref="ArgumentException">Thrown when the value type is unsupported.</exception>
        private ValidationState ValidateBetween(object? value, int min, int max, bool inclusive)
        {
            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            bool isOutOfRange = value switch
            {
                string stringValue => CheckRange(stringValue.Length, min, max, inclusive),
                int intValue => CheckRange(intValue, min, max, inclusive),
                double doubleValue => CheckRange(doubleValue, min, max, inclusive),
                float floatValue => CheckRange(floatValue, min, max, inclusive),
                decimal decimalValue => CheckRange(decimalValue, min, max, inclusive),
                long longValue => CheckRange(longValue, min, max, inclusive),
                short shortValue => CheckRange(shortValue, min, max, inclusive),
                byte byteValue => CheckRange(byteValue, min, max, inclusive),
                ICollection collectionValue => CheckRange(collectionValue.Count, min, max, inclusive),
                _ => throw new ArgumentException(string.Format(Error.UnsupportedType, value.GetType().Name))
            };

            if (isOutOfRange)
            {
                var errorMessage = inclusive
                    ? Error.InclusiveBetween
                    : Error.ExclusiveBetween;
                return ValidationState.Failure(string.Format(errorMessage, _rule.PropertyName, min, max), _rule.PropertyName, _rule.PropertyType);
            }

            return ValidationState.Success(_rule.PropertyName, _rule.PropertyType);
        }

        /// <summary>
        /// Validates that the specified value satisfies a custom condition.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="condition">The condition to apply to the value.</param>
        /// <param name="errorMessage">The error message to return if validation fails.</param>
        /// <returns>A <see cref="ValidationState"/> representing the result of the validation.</returns>
        /// <exception cref="ArgumentException">Thrown when the value type is unsupported.</exception>
        private ValidationState ValidateSign(object? value, Func<double, bool> condition, string errorMessage)
        {
            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            double? numericValue = value switch
            {
                int intValue => intValue,
                long longValue => longValue,
                short shortValue => shortValue,
                sbyte sbyteValue => sbyteValue,
                float floatValue => floatValue,
                double doubleValue => doubleValue,
                decimal decimalValue => (double)decimalValue,
                TimeSpan timeSpanValue => timeSpanValue.TotalMilliseconds,
                _ => throw new ArgumentException(string.Format(Error.UnsupportedType, value.GetType().Name))
            };

            return numericValue.HasValue && condition(numericValue.Value) ?
                ValidationState.Failure(string.Format(errorMessage, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType) :
                ValidationState.Success(_rule.PropertyName, _rule.PropertyType);
        }

        /// <summary>
        /// Validates that the specified collection satisfies a custom condition.
        /// </summary>
        /// <param name="value">The collection to validate.</param>
        /// <param name="condition">The condition to apply to the collection.</param>
        /// <param name="errorMessage">The error message to return if validation fails.</param>
        /// <returns>A <see cref="ValidationState"/> representing the result of the validation.</returns>
        private ValidationState ValidateCollection(object? value, Func<IEnumerable<TProperty>, bool> condition, string errorMessage)
        {
            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            return value is IEnumerable<TProperty> collectionValue && condition(collectionValue) ?
                ValidationState.Success(_rule.PropertyName, _rule.PropertyType) :
                ValidationState.Failure(string.Format(errorMessage, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);
        }

        private ValidationState ValidateInOrNot(object? value, IEnumerable<object?> values, Func<object?, bool> condition, string errorMessage)
        {
            if (values == null)
                throw new ArgumentException(string.Format(Error.NullArgument, nameof(values)));
            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            return condition(values) ?
                ValidationState.Success(_rule.PropertyName, _rule.PropertyType) :
                ValidationState.Failure(string.Format(errorMessage, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);
        }

        private ValidationState ValidateByBoolCondition(object? valueToCheck, Func<bool> condition, string errorMessage)
        {
            if (valueToCheck == null)
                throw new ArgumentException(string.Format(Error.NullArgument, nameof(valueToCheck)));

            return condition() ?
                ValidationState.Success(_rule.PropertyName, _rule.PropertyType) :
                ValidationState.Failure(string.Format(errorMessage, _rule.PropertyName, valueToCheck), _rule.PropertyName, _rule.PropertyType);
        }

        /// <summary>
        /// Checks if the specified value is within the specified range.
        /// </summary>
        /// <typeparam name="T">The type of the value being checked.</typeparam>
        /// <param name="value">The value to check.</param>
        /// <param name="min">The minimum allowable value.</param>
        /// <param name="max">The maximum allowable value.</param>
        /// <param name="inclusive">Indicates whether the range is inclusive or exclusive.</param>
        /// <returns><c>true</c> if the value is outside the specified range; otherwise, <c>false</c>.</returns>
        private static bool CheckRange<T>(T value, int min, int max, bool inclusive) where T : IComparable
        {
            return inclusive
                ? value.CompareTo(min) < 0 || value.CompareTo(max) > 0
                : value.CompareTo(min) <= 0 || value.CompareTo(max) >= 0;
        }

        /// <summary>
        /// Validates whether the specified card number is valid according to the Luhn algorithm.
        /// </summary>
        /// <param name="cardNumber">The card number to validate.</param>
        /// <returns><c>true</c> if the card number is valid; otherwise, <c>false</c>.</returns>
        private static bool IsLuhnValid(string cardNumber)
        {
            int sum = 0;
            bool alternate = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = cardNumber[i] - '0';

                if (alternate)
                {
                    digit *= 2;

                    if (digit > 9) digit -= 9;
                }

                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

        /// <summary>
        /// Validates that the value's length is greater than or equal to the specified minimum length.
        /// </summary>
        /// <param name="length">The minimum length to compare against.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> MinValue(int length) => ProcessValidation(value =>
            ValidateLength(value, length, (actual) => actual >= length, Error.MinLength));

        /// <summary>
        /// Validates that the value's length is less than or equal to the specified maximum length.
        /// </summary>
        /// <param name="length">The maximum length to compare against.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> MaxValue(int length) => ProcessValidation(value =>
            ValidateLength(value, length, (actual) => actual <= length, Error.MaxLength));

        /// <summary>
        /// Validates that the value's length is exactly equal to the specified length.
        /// </summary>
        /// <param name="length">The exact length to compare against.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> Length(int length) => ProcessValidation(value =>
            ValidateLength(value, length, (actual) => actual == length, Error.Length));

        /// <summary>
        /// Validates that the value's date is before the specified date.
        /// </summary>
        /// <param name="before">The date that the value should be before.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> Before(DateTime before) =>
            ValidateDate(before, (date, limit) => date <= limit, Error.DateBefore);

        /// <summary>
        /// Validates that the value's date is after the specified date.
        /// </summary>
        /// <param name="after">The date that the value should be after.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> After(DateTime after) =>
            ValidateDate(after, (date, limit) => date >= limit, Error.DateAfter);

        /// <summary>
        /// Validates that the value is not positive (i.e., less than or equal to zero).
        /// </summary>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> NotPositive() => ProcessValidation(value =>
            ValidateSign(value, x => x > 0, Error.NotPositive));

        /// <summary>
        /// Validates that the value is not negative (i.e., greater than or equal to zero).
        /// </summary>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> NotNegative() => ProcessValidation(value =>
            ValidateSign(value, x => x < 0, Error.NotNegative));

        /// <summary>
        /// Validates that all elements in the collection satisfy the specified condition.
        /// </summary>
        /// <param name="rule">A function representing the condition that all elements must satisfy.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> All(Func<TProperty, bool> rule) => ProcessValidation(value =>
            ValidateCollection(value, x => x.All(rule), Error.All));

        /// <summary>
        /// Validates that at least one element in the collection satisfies the specified condition.
        /// </summary>
        /// <param name="rule">A function representing the condition that at least one element must satisfy.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> Any(Func<TProperty, bool> rule) => ProcessValidation(value =>
            ValidateCollection(value, x => x.Any(rule), Error.Any));

        /// <summary>
        /// Validates that all elements in the collection are unique.
        /// </summary>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> Unique() => ProcessValidation(value =>
            ValidateCollection(value, x => x.Distinct().Count() == x.Count(), Error.Unique));

        /// <summary>
        /// Validates that the value is contained within the specified collection of values.
        /// </summary>
        /// <param name="values">The collection of values to check against.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> In(IEnumerable<object?> values) => ProcessValidation(value =>
            ValidateInOrNot(value, values, x => value is IList valueCollection && valueCollection.Contains(values), Error.In));

        /// <summary>
        /// Validates that the value is not contained within the specified collection of values.
        /// </summary>
        /// <param name="values">The collection of values to check against.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> NotIn(IEnumerable<object?> values) => ProcessValidation(value =>
            ValidateInOrNot(value, values, x => value is IList valueCollection && !valueCollection.Contains(values), Error.NotIn));

        /// <summary>
        /// Validates that the value is equal to the specified value.
        /// </summary>
        /// <param name="valueToCheck">The value to compare against.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> EqualTo(object valueToCheck) => ProcessValidation(value =>
            ValidateByBoolCondition(valueToCheck, () => value == valueToCheck, Error.EqualTo));

        /// <summary>
        /// Validates that the value is not equal to the specified value.
        /// </summary>
        /// <param name="valueToCheck">The value to compare against.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> NotEqualTo(object valueToCheck) => ProcessValidation(value =>
            ValidateByBoolCondition(valueToCheck, () => value != valueToCheck, Error.NotEqualTo));

        public ValidationRuleBuilder<TProperty> StartsWith(string valueToCheck) => ProcessValidation(value =>
            ValidateByBoolCondition(valueToCheck, () => value is string stringValue && stringValue.StartsWith(valueToCheck), Error.StartsWith));

        public ValidationRuleBuilder<TProperty> EndsWith(string valueToCheck) => ProcessValidation(value =>
            ValidateByBoolCondition(valueToCheck, () => value is string stringValue && stringValue.EndsWith(valueToCheck), Error.EndsWith));

        /// <summary>
        /// Validates that the value is inclusively between the specified minimum and maximum values.
        /// </summary>
        /// <param name="min">The minimum value to compare against.</param>
        /// <param name="max">The maximum value to compare against.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> InclusiveBetween(int min, int max) =>
            ProcessValidation(value => ValidateBetween(value, min, max, inclusive: true));

        /// <summary>
        /// Validates that the value is exclusively between the specified minimum and maximum values.
        /// </summary>
        /// <param name="min">The minimum value to compare against.</param>
        /// <param name="max">The maximum value to compare against.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> ExclusiveBetween(int min, int max) =>
            ProcessValidation(value => ValidateBetween(value, min, max, inclusive: false));

        /// <summary>
        /// Validates that the value is greater than the specified value.
        /// </summary>
        /// <param name="gt">The value that the validated value should be greater than.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> GreaterThan(int gt) =>
            ProcessValidation(value => CompareValue(value, v => v > gt, gt, Error.GreaterThan));

        /// <summary>
        /// Validates that the value is less than the specified value.
        /// </summary>
        /// <param name="lt">The value that the validated value should be less than.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> LessThan(int lt) =>
            ProcessValidation(value => CompareValue(value, v => v < lt, lt, Error.LessThan));

        /// <summary>
        /// Sets a custom validation function for the value.
        /// </summary>
        /// <param name="validator">A function that performs custom validation and returns a <see cref="ValidationState"/>.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> SetValidator(Func<object?, ValidationState> validator) =>
            ProcessValidation(validator);

        /// <summary>
        /// Validates that the value is not null.
        /// </summary>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> NotNull() => ProcessValidation(value => value == null ?
            ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType) :
            ValidationState.Success(_rule.PropertyName, _rule.PropertyType)
        );

        /// <summary>
        /// Validates that the value contains the specified value.
        /// </summary>
        /// <param name="valueToFind">The value to search for within the property value.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="valueToFind"/> is null.</exception>
        public ValidationRuleBuilder<TProperty> Contains(object valueToFind) => ProcessValidation(value =>
        {
            if (valueToFind == null)
                throw new ArgumentException(string.Format(Error.NullArgument, nameof(valueToFind)));
            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            if (value is string stringValue && stringValue.Contains(valueToFind.ToString()!))
                return ValidationState.Success(_rule.PropertyName, _rule.PropertyType);
            if (value is IEnumerable<object?> collectionValue && collectionValue.First(x => x == valueToFind) != null)
                return ValidationState.Success(_rule.PropertyName, _rule.PropertyType);

            return ValidationState.Failure(string.Format(Error.Contains, _rule.PropertyName, valueToFind), _rule.PropertyName, _rule.PropertyType);
        });

        /// <summary>
        /// Validates that the value is a date within the specified range.
        /// </summary>
        /// <param name="beginningPeriod">The start of the valid date range.</param>
        /// <param name="endPeriod">The end of the valid date range.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when the value is not of type <see cref="DateTime"/> or <see cref="DateOnly"/>.</exception>
        public ValidationRuleBuilder<TProperty> DateInRange(DateTime beginningPeriod, DateTime endPeriod) => ProcessValidation(value =>
        {
            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            DateTime? dateTimeValue = value switch
            {
                DateTime dt => dt,
                DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
                _ => throw new ArgumentException(string.Format(Error.UnsupportedType, value.GetType().Name))
            };

            if (dateTimeValue == null)
                return ValidationState.Failure(string.Format(Error.NotDate, _rule.PropertyName, value), _rule.PropertyName, _rule.PropertyType);

            bool inRange = dateTimeValue.Value >= beginningPeriod && dateTimeValue.Value <= endPeriod;

            return inRange ?
                ValidationState.Success(_rule.PropertyName, _rule.PropertyType) :
                ValidationState.Failure(string.Format(Error.DateOutOfRange, _rule.PropertyName, beginningPeriod, endPeriod), _rule.PropertyName, _rule.PropertyType);
        });

        /// <summary>
        /// Validates that the value is not empty. The value is considered empty if it is null, an empty string, or an empty collection.
        /// </summary>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> NotEmpty() => ProcessValidation(value =>
        {
            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            if (value is string stringValue && string.IsNullOrEmpty(stringValue))
                return ValidationState.Failure(string.Format(Error.EmptyStringProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);
            if (value is ICollection collection && collection.Count == 0)
                return ValidationState.Failure(string.Format(Error.EmptyCollectionProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            return ValidationState.Success(_rule.PropertyName, _rule.PropertyType);
        });

        /// <summary>
        /// Validates that the value is a valid email address.
        /// </summary>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        public ValidationRuleBuilder<TProperty> EmailAddress() => ProcessValidation(value =>
        {
            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);
            if (value is not string stringValue)
                return ValidationState.Failure(string.Format(Error.NotString, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            bool emailAddress = Regex.IsMatch(stringValue, @"^(?!\.)[\w\.-]{1,64}@[a-zA-Z\d-]{1,255}\.[a-zA-Z]{2,}$");

            return emailAddress ?
                ValidationState.Success(_rule.PropertyName, _rule.PropertyType) :
                ValidationState.Failure(string.Format(Error.EmailAddress, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);
        });

        /// <summary>
        /// Validates that the value is a valid credit card number.
        /// </summary>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> to allow for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when the value is not a string or contains non-digit characters.</exception>
        public ValidationRuleBuilder<TProperty> CreditCard() => ProcessValidation(value =>
        {
            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);
            if (value is not string stringValue)
                return ValidationState.Failure(Error.NotString, _rule.PropertyName, _rule.PropertyType);

            stringValue = stringValue.Replace(" ", string.Empty);

            if (!stringValue.All(char.IsDigit))
                return ValidationState.Failure(Error.CardMustContainDigits, _rule.PropertyName, _rule.PropertyType);
            if (stringValue.Length < 13 || stringValue.Length > 19)
                return ValidationState.Failure(Error.CardMustBeBetween, _rule.PropertyName, _rule.PropertyType);
            if (!IsLuhnValid(stringValue))
                return ValidationState.Failure(Error.CardInvalid, _rule.PropertyName, _rule.PropertyType);

            return ValidationState.Success(_rule.PropertyName, _rule.PropertyType);
        });

        /// <summary>
        /// Validates that the value is greater than or equal to the specified value.
        /// </summary>
        /// <param name="gte">The value to compare the property against.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> instance to continue building the validation rule.</returns>
        public ValidationRuleBuilder<TProperty> GreaterThanOrEqualTo(int gte) => ProcessValidation(value =>
        {
            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            if ((value is int intValue && intValue < gte) ||
                (value is double doubleValue && doubleValue < gte) ||
                (value is float floatValue && floatValue < gte) ||
                (value is decimal decimalValue && decimalValue < gte) ||
                (value is long longValue && longValue < gte) ||
                (value is short shortValue && shortValue < gte) ||
                (value is byte byteValue && byteValue < gte))
            {
                return ValidationState.Failure(string.Format(Error.GreaterThanOrEqualTo, _rule.PropertyName, gte), _rule.PropertyName, _rule.PropertyType);
            }

            return ValidationState.Success(_rule.PropertyName, _rule.PropertyType);
        });

        /// <summary>
        /// Validates that the value is less than or equal to the specified value.
        /// </summary>
        /// <param name="lte">The value to compare the property against.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> instance to continue building the validation rule.</returns>
        public ValidationRuleBuilder<TProperty> LessThanOrEqualTo(int lte) => ProcessValidation(value =>
        {
            if (value == null)
                return ValidationState.Failure(string.Format(Error.NullProperty, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            if ((value is int intValue && intValue > lte) ||
                (value is double doubleValue && doubleValue > lte) ||
                (value is float floatValue && floatValue > lte) ||
                (value is decimal decimalValue && decimalValue > lte) ||
                (value is long longValue && longValue > lte) ||
                (value is short shortValue && shortValue > lte) ||
                (value is byte byteValue && byteValue > lte))
            {
                return ValidationState.Failure(string.Format(Error.LessThanOrEqualTo, _rule.PropertyName, lte), _rule.PropertyName, _rule.PropertyType);
            }

            return ValidationState.Success(_rule.PropertyName, _rule.PropertyType);
        });

        /// <summary>
        /// Validates that the value matches the specified regular expression pattern.
        /// </summary>
        /// <param name="pattern">The regular expression pattern to match against.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> instance to continue building the validation rule.</returns>
        public ValidationRuleBuilder<TProperty> Matches(string pattern) => ProcessValidation(value =>
        {
            if (value is string stringValue && !string.IsNullOrEmpty(stringValue) && Regex.IsMatch(stringValue, pattern))
                return ValidationState.Success(_rule.PropertyName, _rule.PropertyType);

            return ValidationState.Failure(string.Format(Error.Matches, _rule.PropertyName, pattern), _rule.PropertyName, _rule.PropertyType);
        });

        /// <summary>
        /// Validates the property based on a condition determined by the specified predicate.
        /// </summary>
        /// <param name="predicate">A predicate that determines whether the property should be validated.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> instance to continue building the validation rule.</returns>
        public ValidationRuleBuilder<TProperty> When(Predicate<object?> predicate) => ProcessValidation(value =>
        {
            if (predicate(value))
                return ValidationState.Success(_rule.PropertyName, _rule.PropertyType);

            return ValidationState.Failure(string.Format(Error.ConditionError, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);
        });

        /// <summary>
        /// Executes a series of actions on the value as long as the specified predicate returns true.
        /// </summary>
        /// <param name="predicate">A predicate that determines when the action should be executed.</param>
        /// <param name="action">An action to be executed on the value while the predicate is true.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> instance to continue building the validation rule.</returns>
        public ValidationRuleBuilder<TProperty> WhileDo(Predicate<object?> predicate, Action<object?> action) => ProcessValidation(value =>
        {
            if (!predicate(value))
                return ValidationState.Failure(string.Format(Error.ConditionError, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);

            while (predicate(value))
                action(value);

            return ValidationState.Success(_rule.PropertyName, _rule.PropertyType);
        });

        /// <summary>
        /// Executes an action if the specified predicate returns true.
        /// </summary>
        /// <param name="predicate">A predicate that determines if the action should be executed.</param>
        /// <param name="action">An action to be executed if the predicate returns true.</param>
        /// <returns>A <see cref="ValidationRuleBuilder{TProperty}"/> instance to continue building the validation rule.</returns>
        public ValidationRuleBuilder<TProperty> IfThen(Predicate<object?> predicate, Action<object?> action) => ProcessValidation(value =>
        {
            if (predicate(value))
            {
                action(value);
                return ValidationState.Success(_rule.PropertyName, _rule.PropertyType);
            }

            return ValidationState.Failure(string.Format(Error.ConditionError, _rule.PropertyName), _rule.PropertyName, _rule.PropertyType);
        });

        /// <summary>
        /// Sets a custom failure message for the validation rule.
        /// </summary>
        /// <param name="message">The custom message to use when validation fails.</param>
        public void WithMessage(string message)
        {
            var validator = _rule.GetValidator();

            if (!validator.IsValid)
                ValidationState.Failure(message, _rule.PropertyName, _rule.PropertyType);
        }
    }
}
