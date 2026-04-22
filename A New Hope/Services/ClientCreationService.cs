using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.Inputs;
using A_New_Hope.Services.Interfaces;
using A_New_Hope.Utilities;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Services
{
    public class ClientCreationService : IClientCreationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientCreationService> _logger;

        public ClientCreationService(
            ApplicationDbContext context,
            ILogger<ClientCreationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DomainUser> CreateClientWithProfileAndHouseholdAsync(
            ClientEntryInput clientInput,
            List<HouseholdMemberEntryInput>? householdInputs = null,
            ulong? actingUserId = null)
        {
            if (clientInput == null)
            {
                throw new ArgumentNullException(nameof(clientInput));
            }

            householdInputs ??= new List<HouseholdMemberEntryInput>();

            NormalizeClient(clientInput);
            NormalizeIncomes(clientInput.Incomes);
            NormalizeHousehold(householdInputs);

            ValidateRequiredFields(clientInput);

            var now = DateTime.UtcNow;

            var client = new DomainUser
            {
                FirstName = clientInput.FirstName!,
                LastName = clientInput.LastName!,
                Email = clientInput.Email,
                PhoneNumber = clientInput.PhoneNumber,
                AddressLine1 = clientInput.AddressLine1,
                AddressLine2 = clientInput.AddressLine2,
                City = clientInput.City,
                State = clientInput.State,
                PostalCode = clientInput.PostalCode,
                DateOfBirth = clientInput.DateOfBirth,
                UserType = UserType.Client,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = actingUserId,
                UpdatedByUserId = actingUserId
            };

            _context.DomainUsers.Add(client);
            await _context.SaveChangesAsync();

            var clientProfile = new ClientProfile
            {
                UserId = client.Id,
                EmploymentStatus = clientInput.EmploymentStatus!.Value,
                IsUnhoused = clientInput.IsUnhoused,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = actingUserId,
                UpdatedByUserId = actingUserId
            };

            _context.ClientProfiles.Add(clientProfile);

            if (clientInput.Incomes != null)
            {
                foreach (var income in clientInput.Incomes.Where(i => i.HasStarted))
                {
                    var clientIncome = new ClientIncome
                    {
                        ClientProfileUserId = client.Id,
                        IncomeType = income.IncomeType!.Value,
                        MonthlyAmount = income.MonthlyAmount ?? 0m,
                        IsActive = income.IsActive,
                        Notes = InputNormalization.NullIfWhiteSpace(income.Notes),
                        CreatedAt = now,
                        UpdatedAt = now,
                        CreatedByUserId = actingUserId,
                        UpdatedByUserId = actingUserId
                    };

                    _context.ClientIncomes.Add(clientIncome);
                }
            }

            foreach (var member in householdInputs.Where(h => h.HasStarted))
            {
                var householdMember = new HouseholdMember
                {
                    ClientUserId = client.Id,
                    FirstName = member.FirstName!,
                    LastName = member.LastName!,
                    DateOfBirth = member.DateOfBirth,
                    ApproximateAge = member.ApproximateAge,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedByUserId = actingUserId,
                    UpdatedByUserId = actingUserId
                };

                _context.HouseholdMembers.Add(householdMember);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created Client Id {ClientId} for {FirstName} {LastName}",
                client.Id,
                client.FirstName,
                client.LastName);

            return client;
        }

        public async Task<ulong> CreateClientAndReturnIdAsync(
            ClientEntryInput clientInput,
            List<HouseholdMemberEntryInput>? householdInputs = null,
            ulong? actingUserId = null)
        {
            var client = await CreateClientWithProfileAndHouseholdAsync(
                clientInput,
                householdInputs,
                actingUserId);

            return client.Id;
        }

        private static void NormalizeClient(ClientEntryInput input)
        {
            input.FirstName = InputNormalization.NullIfWhiteSpace(input.FirstName);
            input.LastName = InputNormalization.NullIfWhiteSpace(input.LastName);
            input.Email = InputNormalization.NullIfWhiteSpace(input.Email);
            input.PhoneNumber = InputNormalization.NullIfWhiteSpace(input.PhoneNumber);
            input.AddressLine1 = InputNormalization.NullIfWhiteSpace(input.AddressLine1);
            input.AddressLine2 = InputNormalization.NullIfWhiteSpace(input.AddressLine2);
            input.City = InputNormalization.NullIfWhiteSpace(input.City);
            input.State = InputNormalization.NullIfWhiteSpace(input.State)?.ToUpperInvariant();
            input.PostalCode = InputNormalization.NullIfWhiteSpace(input.PostalCode);
        }

        private static void NormalizeHousehold(List<HouseholdMemberEntryInput> householdInputs)
        {
            foreach (var member in householdInputs)
            {
                member.FirstName = InputNormalization.NullIfWhiteSpace(member.FirstName);
                member.LastName = InputNormalization.NullIfWhiteSpace(member.LastName);
            }
        }

        private static void ValidateRequiredFields(ClientEntryInput input)
        {
            if (string.IsNullOrWhiteSpace(input.FirstName))
            {
                throw new ArgumentException("Client first name is required.", nameof(input));
            }

            if (string.IsNullOrWhiteSpace(input.LastName))
            {
                throw new ArgumentException("Client last name is required.", nameof(input));
            }

            if (!input.EmploymentStatus.HasValue)
            {
                throw new ArgumentException("Employment status is required.", nameof(input));
            }
        }

        private static void NormalizeIncomes(List<ClientIncomeEntryInput>? incomes)
        {
            if (incomes == null)
            {
                return;
            }

            foreach (var income in incomes)
            {
                income.Notes = InputNormalization.NullIfWhiteSpace(income.Notes);
            }
        }
    }
}