using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Area : BaseEntity
    {
        public Guid AreaId { get; set; }
        public required string Name { get; set; }
        public required string CodePrefix { get; set; }
        public AreaType Type { get; set; } = AreaType.Normal;
        public string? Description { get; set; }
        public AreaStatus Status { get; set; } = AreaStatus.Active;
        public virtual ICollection<Table> Tables { get; set; } = new List<Table>();

        public static Area Create(
            string name,
            string codePrefix,
            AreaType type,
            string? description,
            Guid? createdBy = null
        )
        {
            return new Area
            {
                Name = name,
                CodePrefix = codePrefix,
                Type = type,
                Description = description,
                CreatedBy = createdBy,
            };
        }

        public DomainResult UpdateDetails(
            string name,
            string codePrefix,
            string? description,
            AreaType type,
            Guid? updatedBy = null
        )
        {
            Name = name;
            CodePrefix = codePrefix;
            Description = description;
            Type = type;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }

        public DomainResult Activate(Guid? updatedBy = null)
        {
            Status = AreaStatus.Active;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }

        public DomainResult Deactivate(Guid? updatedBy = null)
        {
            if (Status == AreaStatus.Inactive)
            {
                return DomainResult.Failure(DomainErrors.Area.AlreadyInactive);
            }

            Status = AreaStatus.Inactive;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }

        public DomainResult UpdateStatus(bool isActive, Guid? updatedBy = null)
        {
            return isActive ? Activate(updatedBy) : Deactivate(updatedBy);
        }
    }
}
