using MediatR;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.Application.Commands.Zones;

public class CreateZoneCommandHandler : IRequestHandler<CreateZoneCommand, int>
{
    private readonly IZoneRepository _zoneRepository;

    public CreateZoneCommandHandler(IZoneRepository zoneRepository)
    {
        _zoneRepository = zoneRepository;
    }

    public async Task<int> Handle(CreateZoneCommand request, CancellationToken cancellationToken)
    {
        return await _zoneRepository.CreateZoneAsync(request.ZoneName, request.Description);
    }
}

public class UpdateZoneCommandHandler : IRequestHandler<UpdateZoneCommand, bool>
{
    private readonly IZoneRepository _zoneRepository;

    public UpdateZoneCommandHandler(IZoneRepository zoneRepository)
    {
        _zoneRepository = zoneRepository;
    }

    public async Task<bool> Handle(UpdateZoneCommand request, CancellationToken cancellationToken)
    {
        return await _zoneRepository.UpdateZoneAsync(request.ZoneId, request.ZoneName, request.Description);
    }
}

public class DeleteZoneCommandHandler : IRequestHandler<DeleteZoneCommand, bool>
{
    private readonly IZoneRepository _zoneRepository;

    public DeleteZoneCommandHandler(IZoneRepository zoneRepository)
    {
        _zoneRepository = zoneRepository;
    }

    public async Task<bool> Handle(DeleteZoneCommand request, CancellationToken cancellationToken)
    {
        return await _zoneRepository.DeleteZoneAsync(request.ZoneId);
    }
}
