using System.Security.Cryptography.X509Certificates;
using AppointmentScheduler.Domain.Validation;
using AppointmentScheduler.Domain.Inputs;

namespace AppointmentScheduler.Domain.Entities;

public sealed class Practitioner
{
    public const int FirstNameMaxLength = 200;

    public const int SurnameMaxLength = 200;

    public Guid ID;
    public string FirstName { get; private set; } = string.Empty;

    public string Surname { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public Practitioner()
    {
        ID = Guid.Empty;
    }

    private Practitioner(string firstName, string surname)
    {
        ID = new Guid();
        FirstName = firstName;
        Surname = surname;

    }


    public static Practitioner Create(string firstName, string surname)
    {
        string _firstName = RequiredTextValidator.ValidateAndNormalize(
            firstName,
            nameof(firstName),
            FirstNameMaxLength);
        string _surname = RequiredTextValidator.ValidateAndNormalize(
            surname,
            nameof(surname),
            SurnameMaxLength);

        return new Practitioner(_firstName, _surname);
    }

    
    internal Practitioner Update(PractitionerDetails practitioner)
    {
        string _firstName = RequiredTextValidator.ValidateAndNormalize(
            practitioner.FirstName,
            nameof(FirstName),
            FirstNameMaxLength);
                
        string _surname = RequiredTextValidator.ValidateAndNormalize(
            practitioner.Surname,
            nameof(Surname),
            SurnameMaxLength);

        FirstName = _firstName;
        Surname = _surname;
        IsActive = practitioner.IsActive;

        return this;
    }
}