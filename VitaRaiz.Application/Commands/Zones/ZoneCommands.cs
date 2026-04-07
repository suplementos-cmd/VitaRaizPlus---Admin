using MediatR;

namespace VitaRaiz.Application.Commands.Zones;

public class CreateZoneCommand : IRequest<int>
{
    public string ZoneName { get; set; } = string.Empty;
    public string? ZoneCode { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateZoneCommand : IRequest<bool>
{
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string? ZoneCode { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class DeleteZoneCommand : IRequest<bool>
{
    public int ZoneId { get; set; }
}
