using Microsoft.Extensions.Options;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;

namespace Tansekak.Infrastructure.Services;

public class ConfigService(CurrentAdmissionYearProvider yearProvider, IOptions<TansekakOptions> options) : IConfigService
{
    public async Task<ConfigDto> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var currentYear = await yearProvider.GetCurrentAsync(cancellationToken);

        return new ConfigDto(
            options.Value.AppName,
            currentYear.Year,
            currentYear.MaximumScore,
            TrackHelper.AllTracks);
    }
}
