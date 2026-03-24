using MediatR;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.Application.Queries.Zones;

public class GetZonesQueryHandler : IRequestHandler<GetZonesQuery, List<ZoneDto>>
{
    private readonly IZoneRepository _zoneRepository;

    public GetZonesQueryHandler(IZoneRepository zoneRepository)
    {
        _zoneRepository = zoneRepository;
    }

    public async Task<List<ZoneDto>> Handle(GetZonesQuery request, CancellationToken cancellationToken)
    {
        return await _zoneRepository.GetZonesAsync();
    }
}

public class GetZoneByIdQueryHandler : IRequestHandler<GetZoneByIdQuery, ZoneDto?>
{
    private readonly IZoneRepository _zoneRepository;

    public GetZoneByIdQueryHandler(IZoneRepository zoneRepository)
    {
        _zoneRepository = zoneRepository;
    }

    public async Task<ZoneDto?> Handle(GetZoneByIdQuery request, CancellationToken cancellationToken)
    {
        return await _zoneRepository.GetZoneByIdAsync(request.ZoneId);
    }
}
