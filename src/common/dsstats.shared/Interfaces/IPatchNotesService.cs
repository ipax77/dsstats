using dsstats.shared.PatchNotes;

namespace dsstats.shared.Interfaces;

public interface IPatchNotesService
{
    Task<PatchNotesPage> GetPatchNotes(PatchNotesRequest request, CancellationToken token = default);

    Task<IReadOnlyList<string>> GetUnitNames(Commander commander, CancellationToken token = default);

    Task<IReadOnlyList<DateTime>> GetPatchDates(int count = 12, CancellationToken token = default);
}
