namespace kecas.domain.network;

/// <summary>
/// Repository for zones (FR-07). Implemented by Infrastructure on EF Core.
/// </summary>
public interface IZoneRepository
{
    Task<Zone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Zone>> ListAsync(CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    void Add(Zone zone);
}
