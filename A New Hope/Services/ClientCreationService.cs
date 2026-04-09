using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.Inputs;
using A_New_Hope.Services.Interfaces;
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
            NormalizeHousehold(householdInputs);

            ValidateRequiredFields(clientInput);

            var duplicateExists = await _context.DomainUsers
                .AnyAsync(u =>
                    u.DeletedAt == null &&
                    u.UserType == UserType.Client &&
                    u.Email.ToLower() == clientInput.Email!.ToLower());

            if (duplicateExists)
            {
                throw new InvalidOperationException("A client with this email address already exists.");
            }

            var now = DateTime.UtcNow;

            var client = new DomainUser
            {
                FirstName = clientInput.FirstName,
                LastName = clientInput.LastName,
                Email = clientInput.Email!,
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
                EmploymentStatus = clientInput.EmploymentStatus,
                EarnedIncomeMonthly = clientInput.EarnedIncomeMonthly,
                IsUnhoused = clientInput.IsUnhoused,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = actingUserId,
                UpdatedByUserId = actingUserId
            };

            _context.ClientProfiles.Add(clientProfile);

            foreach (var member in householdInputs.Where(h => h.HasStarted))
            {
                var householdMember = new HouseholdMember
                {
                    ClientUserId = client.Id,
                    FirstName = member.FirstName!,
                    LastName = member.LastName!,
                    DateOfBirth = member.DateOfBirth,
                    AgeAsOfDate = member.AgeAsOfDate,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedByUserId = actingUserId,
                    UpdatedByUserId = actingUserId
                };

                _context.HouseholdMembers.Add(householdMember);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created Client Id {ClientId} with email {ClientEmail}",
                client.Id,
                client.Email);

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
            input.FirstName = NullIfWhiteSpace(input.FirstName);
            input.LastName = NullIfWhiteSpace(input.LastName);
            input.Email = NullIfWhiteSpace(input.Email);
            input.PhoneNumber = NullIfWhiteSpace(input.PhoneNumber);
            input.AddressLine1 = NullIfWhiteSpace(input.AddressLine1);
            input.AddressLine2 = NullIfWhiteSpace(input.AddressLine2);
            input.City = NullIfWhiteSpace(input.City);
            input.State = NullIfWhiteSpace(input.State)?.ToUpperInvariant();
            input.PostalCode = NullIfWhiteSpace(input.PostalCode);
            input.EmploymentStatus = NullIfWhiteSpace(input.EmploymentStatus);
        }

        private static void NormalizeHousehold(List<HouseholdMemberEntryInput> householdInputs)
        {
            foreach (var member in householdInputs)
            {
                member.FirstName = NullIfWhiteSpace(member.FirstName);
                member.LastName = NullIfWhiteSpace(member.LastName);
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

            if (string.IsNullOrWhiteSpace(input.Email))
            {
                throw new ArgumentException("Client email is required.", nameof(input));
            }
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}