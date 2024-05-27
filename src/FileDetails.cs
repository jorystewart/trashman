namespace Trasher;

public class FileDetails(string name, string size, string originalPath, string timeDeleted)
{
    public string Name { get; init; } = name;
    public string Size { get; init; } = size;
    public string OriginalPath { get; init; } = originalPath;
    public string TimeDeleted { get; init; } = timeDeleted;
}