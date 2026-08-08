using POIneer.Server.Application.Regions;
using POIneer.Server.Contracts.Regions;

namespace POIneer.Server.Infrastructure.Providers;

public sealed class HardcodedRegionProvider : IRegionProvider
{
    public IReadOnlyCollection<RegionResponse> GetRegions()
    {
        return
        [
            new RegionResponse
            {
                Id = "berlin",
                Name = "Berlin",
                Country = "Germany",
                Category = "City"
            }
        ];
    }
}