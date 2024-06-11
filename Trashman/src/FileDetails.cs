namespace Trashman;

public class FileDetails
{
    public string? Name { get; set; }
    public string? Size { get; set; }
    public string? OriginalPath { get; set; }
    public string? TimeDeleted { get; set; }


    public FileDetails(string name, string size, string originalPath, string timeDeleted)
    {
        Name = name;
        Size = size;
        OriginalPath = originalPath;
        TimeDeleted = timeDeleted;
    }
    public FileDetails()
    {

    }
}