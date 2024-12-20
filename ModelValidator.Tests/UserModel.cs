namespace ModelValidator.Tests
{
    internal class UserModel : ModelValidator
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string Address { get; set; }
        public decimal Salary { get; set; }

        public override void AddRules()
        {
            RuleFor(() => Name)
                .NotEmpty()
                .MinValue(4)
                .MaxValue(20)
                .WithMessage("уауауауааууаа");

            RuleFor(() => Email)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("уауауауааууаа");
        }
    }
}
