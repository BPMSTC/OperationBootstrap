using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.Inputs;
using A_New_Hope.Services.Interfaces;
using A_New_Hope.Utilities;
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

            var distinctCategoryIds = input.SelectedServiceCategoryIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (!distinctCategoryIds.Any())
            {
                throw new ArgumentException("At least one service category is required.", nameof(input));
            }

            var validCategoryIds = await _context.ServiceCategories
                .Where(c =>
                    c.DeletedAt == null &&
                    c.IsActive &&
                    distinctCategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            if (validCategoryIds.Count != distinctCategoryIds.Count)
            {
                throw new ArgumentException("One or more selected service categories are invalid.", nameof(input));
            }

            var now = DateTime.UtcNow;

            var entity = new ReferringOrganization
            {
                Name = input.Name!,
                PrimaryContactName = input.PrimaryContactName,
                Email = input.Email!,
                PhoneNumber = input.PhoneNumber!,
                AddressLine1 = input.AddressLine1!,
                AddressLine2 = input.AddressLine2,
                City = input.City!,
                State = input.State!,
                PostalCode = input.PostalCode!,
                Notes = input.Notes,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = actingUserId,
                UpdatedByUserId = actingUserId
            };

            _context.ReferringOrganizations.Add(entity);
            await _context.SaveChangesAsync();

            foreach (var categoryId in validCategoryIds)
            {
                _context.ReferringOrganizationServiceCategories.Add(
                    new ReferringOrganizationServiceCategory
                    {
                        ReferringOrganizationId = entity.Id,
                        ServiceCategoryId = categoryId
                    });
            }

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
            input.Name = InputNormalization.NullIfWhiteSpace(input.Name);
            input.PrimaryContactName = InputNormalization.NullIfWhiteSpace(input.PrimaryContactName);
            input.Email = InputNormalization.NullIfWhiteSpace(input.Email);
            input.PhoneNumber = InputNormalization.NullIfWhiteSpace(input.PhoneNumber);
            input.AddressLine1 = InputNormalization.NullIfWhiteSpace(input.AddressLine1);
            input.AddressLine2 = InputNormalization.NullIfWhiteSpace(input.AddressLine2);
            input.City = InputNormalization.NullIfWhiteSpace(input.City);
            input.State = InputNormalization.NullIfWhiteSpace(input.State)?.ToUpperInvariant();
            input.PostalCode = InputNormalization.NullIfWhiteSpace(input.PostalCode);
            input.Notes = InputNormalization.NullIfWhiteSpace(input.Notes);

            input.SelectedServiceCategoryIds ??= new List<ulong>();
        }

        private static void ValidateRequiredFields(ReferringOrganizationEntryInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
            {
                throw new ArgumentException("Organization name is required.", nameof(input));
            }

            if (string.IsNullOrWhiteSpace(input.PhoneNumber))
            {
                throw new ArgumentException("Phone number is required.", nameof(input));
            }

            if (string.IsNullOrWhiteSpace(input.Email))
            {
                throw new ArgumentException("Email address is required.", nameof(input));
            }

            if (string.IsNullOrWhiteSpace(input.AddressLine1))
            {
                throw new ArgumentException("Address line 1 is required.", nameof(input));
            }

            if (string.IsNullOrWhiteSpace(input.City))
            {
                throw new ArgumentException("City is required.", nameof(input));
            }

            if (string.IsNullOrWhiteSpace(input.State))
            {
                throw new ArgumentException("State is required.", nameof(input));
            }

            if (string.IsNullOrWhiteSpace(input.PostalCode))
            {
                throw new ArgumentException("Postal code is required.", nameof(input));
            }
        }
    }
}