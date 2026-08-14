namespace CampoMarket.Web.Services;

public static class EmailTemplateLoader
{
    public static async Task<string> LoadAsync(
        string templatePath,
        Dictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        var html = await File.ReadAllTextAsync(templatePath, cancellationToken);

        foreach (var pair in values)
        {
            html = html.Replace($"{{{{{pair.Key}}}}}", pair.Value);
        }

        return html;
    }
}
