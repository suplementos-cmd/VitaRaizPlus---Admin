using MediatR;
using VitaRaiz.Application.DTOs;

namespace VitaRaiz.Application.Queries.Zones;

public class GetZonesQuery : IRequest<List<ZoneDto>>
{
}

public class GetZoneByIdQuery : IRequest<ZoneDto?>
{
    public int ZoneId { get; set; }
}
