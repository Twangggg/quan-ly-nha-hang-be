using FoodHub.Application.Interfaces.Branding;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure.Services.Branding
{
    public class BrandingSettingsProvider : IBrandingSettingsProvider
    {
        private readonly IUnitOfWork _unitOfWork;

        public BrandingSettingsProvider(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BrandingSettings> GetOrCreateAsync(CancellationToken cancellationToken = default)
        {
            var repo = _unitOfWork.Repository<BrandingSettings>();
            var settings = await repo.Query()
                .FirstOrDefaultAsync(x => x.SettingsKey == BrandingSettings.DefaultSettingsKey, cancellationToken);

            if (settings != null)
            {
                return settings;
            }

            settings = BrandingSettings.CreateDefault();
            await repo.AddAsync(settings);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            return settings;
        }
    }
}
