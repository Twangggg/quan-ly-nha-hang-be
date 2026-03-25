using System;
using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class InventorySettings : BaseEntity
    {
        public const string DefaultSettingsKey = "inventory";
        public const int DefaultExpiryWarningDays = 7;
        public const decimal DefaultLowStockThresholdValue = 0;
        public const bool DefaultAutoDeductOnCompleted = true;
        public const int DefaultMaxCostRecalcDays = 31;
        public const int DefaultOpeningStockImportCooldownHours = 0;

        protected InventorySettings() { }

        public Guid InventorySettingsId { get; private set; }
        public string SettingsKey { get; private set; } = DefaultSettingsKey;
        public int ExpiryWarningDays { get; private set; }
        public decimal DefaultLowStockThreshold { get; private set; }
        public bool AutoDeductOnCompleted { get; private set; }
        public InventoryCostMethod CostMethod { get; private set; }
        public int MaxCostRecalcDays { get; private set; }
        public int OpeningStockImportCooldownHours { get; private set; }
        public OpeningStockStatus OpeningStockStatus { get; private set; }
        public DateTime? LockedAt { get; private set; }
        public DateTime? LastOpeningStockImportedAt { get; private set; }

        public static InventorySettings CreateDefault(Guid? createdBy = null)
        {
            return new InventorySettings
            {
                InventorySettingsId = Guid.NewGuid(),
                SettingsKey = DefaultSettingsKey,
                ExpiryWarningDays = DefaultExpiryWarningDays,
                DefaultLowStockThreshold = DefaultLowStockThresholdValue,
                AutoDeductOnCompleted = DefaultAutoDeductOnCompleted,
                CostMethod = InventoryCostMethod.WeightedAverage,
                MaxCostRecalcDays = DefaultMaxCostRecalcDays,
                OpeningStockImportCooldownHours = DefaultOpeningStockImportCooldownHours,
                OpeningStockStatus = OpeningStockStatus.Pending,
                LockedAt = null,
                LastOpeningStockImportedAt = null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }

        public DomainResult Update(
            int expiryWarningDays,
            decimal defaultLowStockThreshold,
            bool autoDeductOnCompleted,
            InventoryCostMethod costMethod,
            int maxCostRecalcDays,
            int openingStockImportCooldownHours,
            Guid? updatedBy = null
        )
        {
            if (expiryWarningDays < 1)
            {
                return DomainResult.Failure(
                    DomainErrors.InventorySettings.InvalidExpiryWarningDays
                );
            }

            if (defaultLowStockThreshold < 0)
            {
                return DomainResult.Failure(
                    DomainErrors.InventorySettings.InvalidLowStockThreshold
                );
            }

            if (maxCostRecalcDays < 1 || maxCostRecalcDays > 365)
            {
                return DomainResult.Failure(
                    DomainErrors.InventorySettings.InvalidMaxCostRecalcDays
                );
            }

            if (openingStockImportCooldownHours < 0)
            {
                return DomainResult.Failure(
                    DomainErrors.InventorySettings.InvalidOpeningStockImportCooldownHours
                );
            }

            ExpiryWarningDays = expiryWarningDays;
            DefaultLowStockThreshold = defaultLowStockThreshold;
            AutoDeductOnCompleted = autoDeductOnCompleted;
            CostMethod = costMethod;
            MaxCostRecalcDays = maxCostRecalcDays;
            OpeningStockImportCooldownHours = openingStockImportCooldownHours;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }

        public DomainResult CompleteOpeningStock(Guid? updatedBy = null)
        {
            if (OpeningStockStatus == OpeningStockStatus.Completed && LockedAt.HasValue)
            {
                return DomainResult.Success();
            }

            OpeningStockStatus = OpeningStockStatus.Completed;
            LockedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }

        public void MarkOpeningStockImported(DateTime importedAt, Guid? updatedBy = null)
        {
            LastOpeningStockImportedAt = importedAt;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        public DateTime? GetNextOpeningStockImportAllowedAt()
        {
            if (!LastOpeningStockImportedAt.HasValue || OpeningStockImportCooldownHours <= 0)
            {
                return null;
            }

            return LastOpeningStockImportedAt.Value.AddHours(OpeningStockImportCooldownHours);
        }

        public bool IsOpeningStockImportAllowedAt(DateTime currentTime)
        {
            var nextAllowedAt = GetNextOpeningStockImportAllowedAt();
            return !nextAllowedAt.HasValue || currentTime >= nextAllowedAt.Value;
        }
    }
}
