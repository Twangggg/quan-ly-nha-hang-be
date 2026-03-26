using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure.Services.Reservations
{
    public class ReservationSettingsProvider : IReservationSettingsProvider
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReservationSettingsProvider(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ReservationSettings> GetOrCreateAsync(
            CancellationToken cancellationToken = default
        )
        {
            var repo = _unitOfWork.Repository<ReservationSettings>();

            var settings = await repo.Query()
                .FirstOrDefaultAsync(
                    x => x.SettingsKey == ReservationSettings.DefaultSettingsKey,
                    cancellationToken
                );

            if (settings != null)
            {
                return settings;
            }

            settings = ReservationSettings.CreateDefault();
            await repo.AddAsync(settings);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            return settings;
        }
    }
}
