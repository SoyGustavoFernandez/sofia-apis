namespace SOFIA.Application.Common.Excel;

// Case-insensitive name to id map for bulk loads; repeated names are kept as ambiguous instead of throwing like ToDictionary
public sealed class ImportNameLookup
{
    private readonly Dictionary<string, Guid?> _ids = new(StringComparer.OrdinalIgnoreCase);

    public ImportNameLookup(IEnumerable<(string Name, Guid Id)> items)
    {
        foreach (var (name, id) in items)
        {
            _ids[name] = _ids.ContainsKey(name) ? null : id;
        }
    }

    public bool Contains(string? name) => name is not null && _ids.ContainsKey(name);

    public bool IsAmbiguous(string? name) => name is not null && _ids.TryGetValue(name, out var id) && id is null;

    public bool TryGetUnique(string? name, out Guid id)
    {
        id = Guid.Empty;
        if (name is null || !_ids.TryGetValue(name, out var found) || found is null)
        {
            return false;
        }

        id = found.Value;
        return true;
    }
}
