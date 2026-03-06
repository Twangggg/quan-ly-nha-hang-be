using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
namespace FoodHub.Application.Features.Areas.Queries.GetAreaById
{
    /// <summary>
    /// Thông tin chi tiết của một khu vực.
    /// </summary>
    public class GetAreaByIdResponse : IMapFrom<Area>
    {
        /// <summary>ID duy nhất của khu vực.</summary>
        public Guid AreaId { get; set; }

        /// <summary>Tên khu vực.</summary>
        public required string Name { get; set; }

        /// <summary>Mã tiền tố bàn trong khu vực.</summary>
        public required string CodePrefix { get; set; }

        /// <summary>Loại khu vực.</summary>
        public AreaType Type { get; set; }

        /// <summary>Mô tả khu vực.</summary>
        public string? Description { get; set; }

        /// <summary>Trạng thái hoạt động của khu vực.</summary>
        public AreaStatus Status { get; set; }

        /// <summary>Thời điểm tạo.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Thời điểm cập nhật gần nhất.</summary>
        public DateTime UpdatedAt { get; set; }
        public int NumberOfTables { get; set; }
        public void Mapping(Profile profile)
        {
            profile.CreateMap<Area, GetAreaByIdResponse>()
                .ForMember(d => d.NumberOfTables, opt => opt.MapFrom(s => s.Tables.Count));
        }
    }
}
