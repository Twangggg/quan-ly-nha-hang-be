using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Kds;
using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodHub.Infrastructure.Services.Kds
{
    public class KdsSettingsProvider : IKdsSettingsProvider
    {
        private readonly IUnitOfWork _unitOfWork;

        public KdsSettingsProvider(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<KdsSettings> GetOrCreateAsync(CancellationToken cancellationToken = default)
        {
            var repo = _unitOfWork.Repository<KdsSettings>();
            var settings = await repo.Query()
                .Include(x => x.StationWipLimits)
                .FirstOrDefaultAsync(
                    x => x.SettingsKey == KdsSettings.DefaultSettingsKey,
                    cancellationToken
                );

            if (settings == null)
            {
                settings = KdsSettings.CreateDefault();
                await repo.AddAsync(settings);
                try
                {
                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                    return settings;
                }
                catch (DbUpdateException ex) when (IsDuplicateSettingsKey(ex))
                {
                    _unitOfWork.ClearChangeTracker();

                    settings = await repo.Query()
                        .Include(x => x.StationWipLimits)
                        .FirstOrDefaultAsync(
                            x => x.SettingsKey == KdsSettings.DefaultSettingsKey,
                            cancellationToken
                        );

                    if (settings == null)
                    {
                        throw;
                    }

                    if (settings.EnsureDefaultStationWipLimits())
                    {
                        await _unitOfWork.SaveChangeAsync(cancellationToken);
                    }

                    return settings;
                }
            }

            if (settings.EnsureDefaultStationWipLimits())
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }

            return settings;
        }

        private static bool IsDuplicateSettingsKey(DbUpdateException exception)
        {
            var postgresException = exception.InnerException as PostgresException;
            return postgresException?.SqlState == PostgresErrorCodes.UniqueViolation
                && postgresException.ConstraintName == "ix_kds_settings_settings_key";
        }
    }
}
