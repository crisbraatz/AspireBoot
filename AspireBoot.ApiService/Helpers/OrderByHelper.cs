namespace AspireBoot.ApiService.Helpers;

public static class OrderByHelper
{
    public static IDictionary<string, bool> ToDictionary<T>(string? orderBy)
    {
        HashSet<string> propertiesName = typeof(T).GetProperties().Select(x => x.Name.ToUpperInvariant()).ToHashSet();

        orderBy ??= $"{propertiesName.FirstOrDefault()} ASC";

        Dictionary<string, bool> dictionary = [];

        foreach (string value in orderBy.Split(';'))
        {
            string[] data = value.Split(' ');

            string? propertyName = data.FirstOrDefault()?.Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(propertyName) && propertiesName.Contains(propertyName))
                dictionary.Add(propertyName, data.LastOrDefault()?.Trim().ToUpperInvariant() switch
                {
                    "ASC" => true,
                    "DESC" => false,
                    _ => true
                });
        }

        return dictionary;
    }
}
