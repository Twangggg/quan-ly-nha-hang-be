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

        /// <summary>
        /// Cập nhật thông tin khu vực (Name, Description, Type).
        /// </summary>
        public void Update(string name, string? description, AreaType type)
        {
            Name = name;
            Description = description;
            Type = type;
        }

        /// <summary>
        /// Cập nhật trạng thái hoạt động của khu vực.
        /// </summary>
        public void UpdateStatus(bool isActive, Guid? updatedBy = null)
        {
            Status = isActive ? AreaStatus.Active : AreaStatus.Inactive;
            UpdatedAt = DateTime.UtcNow;
            if (updatedBy.HasValue)
                UpdatedBy = updatedBy.Value;
        }
    }
}
