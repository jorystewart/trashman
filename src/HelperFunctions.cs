using System.Text;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Trasher;

public class HelperFunctions
{
  public static string ConvertBytes(long bytes)
  {
    switch (bytes)
    {
      case < 1000:
        return (bytes.ToString() + " B");
      case < 1000000:
        return (bytes / 1000).ToString() + " KB";
      case < 1000000000:
        return (bytes / 1000000).ToString() + " MB";
      default:
        return (bytes / 1000000000).ToString() + " GB";
    }
  }


  public static void WriteConsoleTable(List<FileDetails> list)
  {
    int consoleWidth = Console.WindowWidth;

    int nameColumnWidth = list.Max(r => r.Name.Length) + 2;
    string nameColumnHeader = "Name";
    if (nameColumnWidth < nameColumnHeader.Length) { nameColumnWidth = nameColumnHeader.Length + 2; }

    int sizeColumnWidth = 7 + 2;
    string sizeColumnHeader = "Size";

    int timeDeletedColumnWidth = list.Max(r => r.TimeDeleted.Length) + 2;
    string timeDeletedColumnHeader = "Time Deleted";
    if (timeDeletedColumnWidth < timeDeletedColumnHeader.Length) { timeDeletedColumnWidth = timeDeletedColumnHeader.Length + 2; }

    int originalPathColumnWidth = list.Max(r => r.OriginalPath.Length) + 2;
    string originalPathColumnHeader = "Original Path";
    if (originalPathColumnWidth < originalPathColumnHeader.Length) { originalPathColumnWidth = originalPathColumnHeader.Length + 2; }

    Dictionary<string, int> columnHeaderInfo = new Dictionary<string, int>();

    if (consoleWidth > nameColumnWidth + 2)
    {
      columnHeaderInfo.Add(nameColumnHeader, nameColumnWidth);
    }

    if (consoleWidth > (nameColumnWidth + sizeColumnWidth + 3))
    {
      columnHeaderInfo.Add(sizeColumnHeader, sizeColumnWidth);
    }

    if (consoleWidth > (nameColumnWidth + sizeColumnWidth + originalPathColumnWidth + 4))
    {
      columnHeaderInfo.Add(originalPathColumnHeader, originalPathColumnWidth);
    }

    if (consoleWidth > (nameColumnWidth + sizeColumnWidth + originalPathColumnWidth + timeDeletedColumnWidth + 5))
    {
      columnHeaderInfo.Add(timeDeletedColumnHeader, timeDeletedColumnWidth);
    }

    WriteColumnHeaders(columnHeaderInfo);

    columnHeaderInfo = columnHeaderInfo.ToDictionary(
      keyValue => keyValue.Key.Replace(" ", ""),
      keyValue => keyValue.Value
    );

    WriteTableInfo(list, columnHeaderInfo);

  }

  private static void WriteColumnHeaders(Dictionary<string, int> columnsInfo)
  {
    StringBuilder builder = new StringBuilder();

    foreach (KeyValuePair<string, int> column in columnsInfo)
    {
      builder.Append('+');
      builder.Append('-', column.Value);
    }

    builder.Append('+');
    Console.WriteLine(builder.ToString());
    builder.Clear();

    foreach (KeyValuePair<string, int> column in columnsInfo)
    {
      int totalPadding = column.Value - column.Key.Length;
      int leftPadding = totalPadding / 2;
      int rightPadding = (BitOperations.TrailingZeroCount(totalPadding) == 0)
        ? (totalPadding / 2) + 1
        : totalPadding / 2;

      builder.Append('|');
      builder.Append(' ', leftPadding);
      builder.Append(column.Key);
      builder.Append(' ', rightPadding);
    }

    builder.Append('|');
    Console.WriteLine(builder.ToString());
    builder.Clear();

    foreach (KeyValuePair<string, int> column in columnsInfo)
    {
      builder.Append('+');
      builder.Append('-', column.Value);
    }

    builder.Append('+');
    Console.WriteLine(builder.ToString());

  }

  private static void WriteTableInfo(List<FileDetails> list, Dictionary<string, int> columnData)
  {
    StringBuilder builder = new StringBuilder();

    foreach (FileDetails item in list)
    {
      builder.Clear();
      foreach (PropertyInfo property in (typeof(FileDetails).GetProperties()))
      {
        if (columnData.Keys.Contains(property.Name))
        {
          builder.Append('|');
          builder.Append(' ');
          builder.Append(property.GetValue(item));

          bool result = columnData.TryGetValue(property.Name, out int keyValue);

          if (result)
          {
            int rightPadding = keyValue - property.GetValue(item).ToString().Length - 1;
            builder.Append(' ', rightPadding);
          }
        }
      }
      builder.Append('|');
      Console.WriteLine(builder.ToString());
    }

    builder.Clear();

    foreach (KeyValuePair<string, int> kvp in columnData)
    {
      builder.Append('+');
      builder.Append('-', kvp.Value);
    }

    builder.Append('+');
    Console.WriteLine(builder.ToString());
  }

  public static string[] ExpandPath(string inputPath)
  {
    return new string[5];
  }

  public static string ProcessInputPathString(string inputPath)
  {
    Regex leadingDotMatch = new Regex(@"^\.[\\/]"); // starts with ./ or .\
    Regex leadingDoubleDotMatch = new Regex(@"^\.{2}[\\/]"); // starts with ../ or ..\
    Regex leadingTildeMatch = new Regex(@"^~[\\/]"); // starts with ~/ or ~\
    Regex dotMatch = new Regex(@"[\\/]\.[\\/]"); // contains a dot surrounded by forward or back slashes
    Regex doubleDotMatch = new Regex(@"[\\/].{2}[\\/]"); // contains 2 dots surrounded by forward or back slashes
    Regex tildeMatch = new Regex(@"[\\/]~[\\/]"); // contains a tilde surrounded by forward or back slashes
    Regex doubleDotReplacementTarget = new Regex(@"[^\\/]*[\\/]\.{2}[\\/]");


    if (!inputPath.Contains('/') && !inputPath.Contains('\\'))
    {
      inputPath = Directory.GetCurrentDirectory() + Path.AltDirectorySeparatorChar + inputPath;
    }
    if (leadingDoubleDotMatch.IsMatch(inputPath))
    {
      inputPath = leadingDoubleDotMatch.Replace(inputPath, new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.FullName + Path.AltDirectorySeparatorChar, 1);
    }
    else if (leadingDotMatch.IsMatch(inputPath))
    {
      inputPath = leadingDotMatch.Replace(inputPath, Directory.GetCurrentDirectory() + Path.AltDirectorySeparatorChar, 1);
    }

    if (leadingTildeMatch.IsMatch(inputPath))
    {
      inputPath = leadingTildeMatch.Replace(inputPath,  Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH") + Path.AltDirectorySeparatorChar, 1);
    }

    if (tildeMatch.IsMatch(inputPath)) // if for some reason /~/ is still in the path, replace with /
    {
      inputPath = tildeMatch.Replace(inputPath, Path.AltDirectorySeparatorChar.ToString());
    }

    if (dotMatch.IsMatch(inputPath)) // if /./ is in path, just replace with /
    {
      inputPath = dotMatch.Replace(inputPath, Path.AltDirectorySeparatorChar.ToString());
    }

    if (doubleDotMatch.IsMatch(inputPath))
    {
      inputPath = doubleDotReplacementTarget.Replace(inputPath, String.Empty);
    }

    inputPath = inputPath.Replace('\\', '/');

    return inputPath;
  }

}