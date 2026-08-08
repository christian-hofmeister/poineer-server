using POIneer.Server.Contracts.Regions;

namespace POIneer.Server.Application.Regions;

public interface IRegionProvider
{
    IReadOnlyCollection<RegionResponse> GetRegions();
}