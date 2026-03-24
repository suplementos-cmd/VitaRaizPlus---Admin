using MediatR;

namespace VitaRaiz.Application.Commands.Zones;

public class CreateZoneCommand : IRequest<int>
{
    public string ZoneName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateZoneCommand : IRequest<bool>
{
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class DeleteZoneCommand : IRequest<bool>
{
    public int ZoneId { get; set; }
}
