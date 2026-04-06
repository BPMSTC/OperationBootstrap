using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.ViewModels.Referrals;
using A_New_Hope.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Services
{
    public class ReferringOrganizationService : IReferringOrganizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReferringOrganizationService> _logger;

        public ReferringOrganizationService(
            ApplicationDbContext context,
            ILogger<ReferringOrganizationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ReferringOrganization> CreateAsync(
            ReferringOrganizationEntryInput input,
            ulong? actingUserId = null)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            Normalize(input);

            ValidateRequiredFields(input);

            var duplicateExists = await _context.ReferringOrganizations
                .AnyAsync(o =>
                    o.DeletedAt == null &&
                    o.Name.ToLower() == input.Name!.ToLower());

            if (duplicateExists)
            {
                throw new InvalidOperationException("An organization with this name already exists.");
            }

            var now = DateTime.UtcNow;

            var entity = new ReferringOrganization
            {
                Name = input.Name!,
                Type = input.Type,
                PrimaryContactName = input.PrimaryContactName,
                Email = input.Email,
                PhoneNumber = input.PhoneNumber,
                AddressLine1 = input.AddressLine1,
                AddressLine2 = input.AddressLine2,
                City = input.City,
                State = input.State,
                PostalCode = input.PostalCode,
                Notes = input.Notes,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = actingUserId,
                UpdatedByUserId = actingUserId
            };

            _context.ReferringOrganizations.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created ReferringOrganization Id {OrganizationId} with name {OrganizationName}",
                entity.Id,
                entity.Name);

            return entity;
        }

        public async Task<ulong> CreateAndReturnIdAsync(
            ReferringOrganizationEntryInput input,
            ulong? actingUserId = null)
        {
            var entity = await CreateAsync(input, actingUserId);
            return entity.Id;
        }

        private static void Normalize(ReferringOrganizationEntryInput input)
        {
            input.Name = NullIfWhiteSpace(input.Name);
            input.Type = NullIfWhiteSpace(input.Type);
            input.PrimaryContactName = NullIfWhiteSpace(input.PrimaryContactName);
            input.Email = NullIfWhiteSpace(input.Email);
            input.PhoneNumber = NullIfWhiteSpace(input.PhoneNumber);
            input.AddressLine1 = NullIfWhiteSpace(input.AddressLine1);
            input.AddressLine2 = NullIfWhiteSpace(input.AddressLine2);
            input.City = NullIfWhiteSpace(input.City);
            input.State = NullIfWhiteSpace(input.State)?.ToUpperInvariant();
            input.PostalCode = NullIfWhiteSpace(input.PostalCode);
            input.Notes = NullIfWhiteSpace(input.Notes);
        }

        private static void ValidateRequiredFields(ReferringOrganizationEntryInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
            {
                throw new ArgumentException("Organization name is required.", nameof(input));
            }
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}