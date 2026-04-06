using A_New_Hope.Data;
using A_New_Hope.Models;
using A_New_Hope.Models.Inputs;
using A_New_Hope.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace A_New_Hope.Services
{
    public class ReferralService : IReferralService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReferralService> _logger;

        public ReferralService(
            ApplicationDbContext context,
            ILogger<ReferralService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Referral> CreateAsync(
            ReferralDetailsInput input,
            ulong clientUserId,
            ulong referringOrganizationId,
            ulong? actingUserId = null)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            Normalize(input);
            ValidateRequiredFields(input);

            var clientExists = await _context.DomainUsers.AnyAsync(u =>
                u.Id == clientUserId &&
                u.DeletedAt == null &&
                u.UserType == UserType.Client &&
                u.IsActive);

            if (!clientExists)
            {
                throw new InvalidOperationException("The selected client is not valid.");
            }

            var organizationExists = await _context.ReferringOrganizations.AnyAsync(o =>
                o.Id == referringOrganizationId &&
                o.DeletedAt == null &&
                o.IsActive);

            if (!organizationExists)
            {
                throw new InvalidOperationException("The selected referring organization is not valid.");
            }

            var now = DateTime.UtcNow;

            var entity = new Referral
            {
                ClientUserId = clientUserId,
                ReferringOrganizationId = referringOrganizationId,
                ReferredOn = input.ReferredOn!.Value,
                Status = input.Status!.Value,
                ValidFrom = input.ValidFrom,
                ValidTo = input.ValidTo,
                ReferredByName = input.ReferredByName,
                ReferredByPhoneNumber = input.ReferredByPhoneNumber,
                ReferredByEmail = input.ReferredByEmail,
                Notes = input.Notes,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = actingUserId,
                UpdatedByUserId = actingUserId
            };

            _context.Referrals.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created Referral Id {ReferralId} for ClientUserId {ClientUserId} and ReferringOrganizationId {ReferringOrganizationId}",
                entity.Id,
                clientUserId,
                referringOrganizationId);

            return entity;
        }

        public async Task<ulong> CreateAndReturnIdAsync(
            ReferralDetailsInput input,
            ulong clientUserId,
            ulong referringOrganizationId,
            ulong? actingUserId = null)
        {
            var entity = await CreateAsync(input, clientUserId, referringOrganizationId, actingUserId);
            return entity.Id;
        }

        private static void Normalize(ReferralDetailsInput input)
        {
            input.ReferredByName = NullIfWhiteSpace(input.ReferredByName);
            input.ReferredByPhoneNumber = NullIfWhiteSpace(input.ReferredByPhoneNumber);
            input.ReferredByEmail = NullIfWhiteSpace(input.ReferredByEmail);
            input.Notes = NullIfWhiteSpace(input.Notes);
        }

        private static void ValidateRequiredFields(ReferralDetailsInput input)
        {
            if (!input.ReferredOn.HasValue)
            {
                throw new ArgumentException("Referral date is required.", nameof(input));
            }

            if (!input.Status.HasValue)
            {
                throw new ArgumentException("Referral status is required.", nameof(input));
            }
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}