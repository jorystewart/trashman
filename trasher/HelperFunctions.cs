using System.ComponentModel;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Numerics;
using System.Reflection;

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
    //int columnOneWidth = (list.Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur).Length);
    int nameColumnWidth = list.Max(r => r.Name.Length) + 2;
    string nameColumnHeader = "Name";
    if (nameColumnWidth < nameColumnHeader.Length) { nameColumnWidth = nameColumnHeader.Length + 2; }
    int nameHeaderPadding = nameColumnWidth - nameColumnHeader.Length;
    int nameHeaderLeftPadding = nameHeaderPadding / 2;
    int nameHeaderRightPadding = (BitOperations.TrailingZeroCount(nameHeaderPadding) == 0)
      ? (nameHeaderPadding / 2) + 1
      : nameHeaderPadding / 2;

    int sizeColumnWidth = 7 + 2;
    string sizeColumnHeader = "Size";
    int sizeHeaderPadding = sizeColumnWidth - sizeColumnHeader.Length;
    int sizeHeaderLeftPadding = sizeHeaderPadding / 2;
    int sizeHeaderRightPadding = (BitOperations.TrailingZeroCount(sizeHeaderPadding) == 0)
      ? (sizeHeaderPadding / 2) + 1
      : (sizeHeaderPadding / 2);

    int timeDeletedColumnWidth = list.Max(r => r.TimeDeleted.Length) + 2;
    string timeDeletedColumnHeader = "Deletion Time";
    if (timeDeletedColumnWidth < timeDeletedColumnHeader.Length) { timeDeletedColumnWidth = timeDeletedColumnHeader.Length + 2; }
    int timeDeletedHeaderPadding = timeDeletedColumnWidth - timeDeletedColumnHeader.Length;
    int timeDeletedHeaderLeftPadding = timeDeletedHeaderPadding / 2;
    int timeDeletedHeaderRightPadding = (BitOperations.TrailingZeroCount(timeDeletedHeaderPadding) == 0)
      ? (timeDeletedHeaderPadding / 2) + 1
      : timeDeletedHeaderPadding / 2;

    int originalPathColumnWidth = list.Max(r => r.OriginalPath.Length) + 2;
    string originalPathColumnHeader = "Original Path";
    if (originalPathColumnWidth < originalPathColumnHeader.Length) { originalPathColumnWidth = originalPathColumnHeader.Length + 2; }
    int originalPathHeaderPadding = originalPathColumnWidth - originalPathColumnHeader.Length;
    int originalPathHeaderLeftPadding = originalPathHeaderPadding / 2;
    int originalPathHeaderRightPadding = (BitOperations.TrailingZeroCount(originalPathHeaderPadding) == 0)
      ? (originalPathHeaderPadding / 2) + 1
      : originalPathHeaderPadding / 2;


    int tableWidth = nameColumnWidth + sizeColumnWidth + timeDeletedColumnWidth + originalPathColumnWidth + 13;
    StringBuilder builder = new StringBuilder();
    builder.Append('+');
    builder.Append('-', nameColumnWidth);
    builder.Append('+');
    builder.Append('-', sizeColumnWidth);
    builder.Append('+');
    builder.Append('-', originalPathColumnWidth);
    builder.Append('+');
    builder.Append('-', timeDeletedColumnWidth);
    builder.Append('+');
    Console.WriteLine(builder.ToString());
    builder.Clear();
    builder.Append('|');
    builder.Append(' ', nameHeaderLeftPadding);
    builder.Append(nameColumnHeader);
    builder.Append(' ', nameHeaderRightPadding);
    builder.Append('|');
    builder.Append(' ', sizeHeaderLeftPadding);
    builder.Append(sizeColumnHeader);
    builder.Append(' ', sizeHeaderRightPadding);
    builder.Append('|');
    builder.Append(' ', originalPathHeaderLeftPadding);
    builder.Append(originalPathColumnHeader);
    builder.Append(' ', originalPathHeaderRightPadding);
    builder.Append('|');
    builder.Append(' ', timeDeletedHeaderLeftPadding);
    builder.Append(timeDeletedColumnHeader);
    builder.Append(' ', timeDeletedHeaderRightPadding);
    builder.Append('|');
    Console.WriteLine(builder.ToString());
    builder.Clear();
    builder.Append('+');
    builder.Append('-', nameColumnWidth);
    builder.Append('+');
    builder.Append('-', sizeColumnWidth);
    builder.Append('+');
    builder.Append('-', originalPathColumnWidth);
    builder.Append('+');
    builder.Append('-', timeDeletedColumnWidth);
    builder.Append('+');
    Console.WriteLine(builder.ToString());

    foreach (FileDetails item in list)
    {
      int leftPadding, rightPadding = 0;
      builder.Clear();

      int nameColumnPadding = nameColumnWidth - (item.Name.Length);
      leftPadding = nameColumnPadding / 2;
      rightPadding = (BitOperations.TrailingZeroCount(nameColumnWidth - (item.Name.Length)) == 0)
        ? (nameColumnPadding / 2) + 1
        : nameColumnPadding / 2;
      builder.Append('|');
      builder.Append(' ', leftPadding);
      builder.Append(item.Name);
      builder.Append(' ', rightPadding);
      builder.Append('|');

      bool parseCheck = Int64.TryParse((item.Size.Split(' ')[0]), out long fileBytes);
      if (!parseCheck)
      {
        throw new Exception();
      }
      string fileSize = ConvertBytes(fileBytes);
      int sizeColumnPadding = sizeColumnWidth - (fileSize.Length);
      leftPadding = sizeColumnPadding / 2;
      rightPadding = (BitOperations.TrailingZeroCount(sizeColumnPadding) == 0)
        ? (sizeColumnPadding / 2) + 1
        : sizeColumnPadding / 2;
      builder.Append(' ', leftPadding);
      builder.Append(fileSize);
      builder.Append(' ', rightPadding);
      builder.Append('|');

      int originalPathColumnPadding = originalPathColumnWidth - (item.OriginalPath.Length);
      leftPadding = originalPathColumnPadding / 2;
      rightPadding = (BitOperations.TrailingZeroCount(originalPathColumnPadding) == 0)
        ? (originalPathColumnPadding / 2) + 1
        : originalPathColumnPadding / 2;
      builder.Append(' ', leftPadding);
      builder.Append(item.OriginalPath);
      builder.Append(' ', rightPadding);
      builder.Append('|');

      int timeDeletedColumnPadding = timeDeletedColumnWidth - (item.TimeDeleted.Length);
      leftPadding = timeDeletedColumnPadding / 2;
      rightPadding = (BitOperations.TrailingZeroCount(timeDeletedColumnPadding) == 0)
        ? (timeDeletedColumnPadding / 2) + 1
        : timeDeletedColumnPadding / 2;
      builder.Append(' ', leftPadding);
      builder.Append(item.TimeDeleted);
      builder.Append(' ', rightPadding);
      builder.Append('|');
      Console.WriteLine(builder.ToString());
    }
    builder.Clear();
    builder.Append('+');
    builder.Append('-', nameColumnWidth);
    builder.Append('+');
    builder.Append('-', sizeColumnWidth);
    builder.Append('+');
    builder.Append('-', originalPathColumnWidth);
    builder.Append('+');
    builder.Append('-', timeDeletedColumnWidth);
    builder.Append('+');
    Console.WriteLine(builder.ToString());
  }
}