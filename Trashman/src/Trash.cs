using System.Collections;
using System.Security;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.CompilerServices;

namespace Trashman;

public class Trash
{
  private static string _trashLocation = (Environment.GetEnvironmentVariable("XDG_DATA_HOME") == (String.Empty) || Environment.GetEnvironmentVariable("XDG_DATA_HOME") == null) ? Environment.GetEnvironmentVariable("HOME") + "/.local/share/Trash" : Environment.GetEnvironmentVariable("XFG_DATA_HOME") + "/Trash";

  public static void TestTrashDirectories()
  {
    if (!Directory.Exists(_trashLocation)) { Directory.CreateDirectory(_trashLocation, (UnixFileMode.UserRead | UnixFileMode.UserWrite)); }
    if (!Directory.Exists(_trashLocation + "/files")) { Directory.CreateDirectory(_trashLocation + "/files"); }
    if (!Directory.Exists(_trashLocation + "/info")) { Directory.CreateDirectory(_trashLocation + "/info"); }
  }

  public static void SendToTrash(FileSystemInfo file)
  {
    TestTrashDirectories();

    string trashFileName;
    FileStream fileStream;
    StreamWriter writer;
    switch (file)
    {
      case FileInfo fileInfo:
        trashFileName = fileInfo.Name;
        if (File.Exists(_trashLocation + "/files/" + trashFileName))
        {
          int counter = 0;
          while (Directory.Exists(_trashLocation + "/files/" + trashFileName))
          {
            trashFileName = trashFileName + counter.ToString();
          }
        }

        fileStream = File.Create(_trashLocation + "/info/" + trashFileName + ".trashinfo");
        writer = new StreamWriter(fileStream);
        writer.AutoFlush = true;
        writer.Write("[Trash Info]" + Environment.NewLine);
        writer.Write("Path=" + fileInfo.FullName + Environment.NewLine);
        writer.Write("DeletionDate=" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + Environment.NewLine);
        writer.Close();
        fileStream.Close();
        File.Move(fileInfo.FullName, _trashLocation + "/files/" + fileInfo.Name);
        break;

      case DirectoryInfo directoryInfo:
        trashFileName = directoryInfo.Name;
        if (Directory.Exists(_trashLocation + "/files/" + trashFileName))
        {
          int counter = 0;
          while (Directory.Exists(_trashLocation + "/files/" + trashFileName))
          {
            trashFileName = trashFileName + counter.ToString();
          }
        }

        fileStream = File.Create(_trashLocation + "/info/" + trashFileName + ".trashinfo");
        writer = new StreamWriter(fileStream);
        writer.Write("[Trash Info]" + Environment.NewLine);
        writer.Write("Path=" + directoryInfo.FullName + Environment.NewLine);
        writer.Write("DeletionDate=" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + Environment.NewLine);
        writer.Close();
        fileStream.Close();
        Directory.Move(directoryInfo.FullName, _trashLocation + "/files/" + directoryInfo.Name);
        break;
    }
  }

  public static void RestoreFromTrash(string file)
  {
    TestTrashDirectories();
    if (file.Contains('/'))
    {
      Console.WriteLine("Invalid character in input: " + file);
      return;
    }

    TestTrashDirectories();
    DirectoryInfo trashInfoDir = new DirectoryInfo(_trashLocation + "/info");
    DirectoryInfo trashFilesDir = new DirectoryInfo(_trashLocation + "/files");
    List<Tuple<FileInfo,FileDetails>> trashContents = new List<Tuple<FileInfo, FileDetails>>();
    FileInfo[] trashInfoFiles = trashInfoDir.GetFiles();
    if (trashInfoFiles.Length <= 0)
    {
      return;
    }

    foreach (FileInfo infoFile in trashInfoFiles)
    {
      FileStream stream = infoFile.Open(FileMode.Open);
      StreamReader reader = new StreamReader(stream);
      FileDetails fileDetails = new FileDetails();
      string? readString;
      while (!reader.EndOfStream)
      {
        readString = reader.ReadLine();
        if (readString == null || readString[0] == '[')
        {
          continue;
        }

        KeyValuePair<string, string> kvp =
          new KeyValuePair<string, string>(readString.Split('=')[0], readString.Split('=')[1]);

        switch (kvp.Key)
        {
          case "Path":
            fileDetails.OriginalPath = kvp.Value;
            fileDetails.Name = kvp.Value.Split('/')[^1];
            break;
          case "DeletionDate":
            string timeString = kvp.Value;
            timeString = timeString.Replace('-', '/');
            timeString = timeString.Replace('T', ' ');
            fileDetails.TimeDeleted = timeString;
            break;
        }
      }

      if (File.Exists(trashFilesDir + "/" + fileDetails.Name))
      {
        fileDetails.Size = HelperFunctions.ConvertBytes((new FileInfo(trashFilesDir.FullName + "/" + fileDetails.Name)).Length);
      }
      else if (Directory.Exists(trashFilesDir + "/" + fileDetails.Name))
      {
        FileSystemInfo[] contents = new DirectoryInfo(trashFilesDir + "/" + fileDetails.Name).GetFileSystemInfos("*", SearchOption.AllDirectories);
        long totalSize = 0;
        foreach (FileSystemInfo item in contents)
        {
          switch (item)
          {
            case FileInfo fileInfo:
              totalSize = totalSize + fileInfo.Length;
              break;
          }
        }

        fileDetails.Size = HelperFunctions.ConvertBytes(totalSize);
      }
      Tuple<FileInfo, FileDetails> trashItem = new Tuple<FileInfo, FileDetails>(infoFile, fileDetails);
      trashContents.Add(trashItem);
    }
    string pattern = file.Replace(@".", @"\.");
    pattern = pattern.Replace("*", @".*");
    Regex starReplace = new Regex($"^{pattern}$");
    IEnumerable<Tuple<FileInfo,FileDetails>> searchResult = from item in (trashContents)
      where starReplace.IsMatch(item.Item2.Name)
      select item;

    foreach (Tuple<FileInfo, FileDetails> item in searchResult)
    {
      if (File.Exists(trashFilesDir + "/" + item.Item2.Name))
      {
        try
        {
          File.Move(trashFilesDir + "/" + item.Item2.Name, item.Item2.OriginalPath);
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException)
        {
          Console.WriteLine("Error: failed to restore " + item.Item2.Name + " - source or destination is null");
          continue;
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          Console.WriteLine("Error: failed to restore " + item.Item2.Name + " - insufficient permissions.");
          continue;
        }
        catch (Exception e) when (e is PathTooLongException)
        {
          Console.WriteLine("Error: failed to restore " + item.Item2.Name +
                            " - source or destination path exceeds system maximum path length");
          continue;
        }
        catch (Exception e) when (e is FileNotFoundException)
        {
          Console.WriteLine("Error: failed to restore " + item.Item2.Name + " - unable to locate file.");
          continue;
        }
        catch (Exception e) when (e is DirectoryNotFoundException or NotSupportedException)
        {
          Console.WriteLine("Error: failed to restore " + item.Item2.Name + " - target or destination is invalid");
          continue;
        }
        catch (Exception e) when (e is IOException)
        {
          Console.WriteLine("Error: failed to restore " + item.Item2.Name + "(IOException)");
          continue;
        }
        catch (Exception e)
        {
          Console.WriteLine("Error: failed to restore " + item.Item2.Name + " - " + e);
          continue;
        }
        try
        {
          item.Item1.Delete();
        }
        catch (Exception e) when (e is SecurityException)
        {
          Console.WriteLine("Error: failed to clean " + item.Item1.Name + " - insufficient permissions");
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          Console.WriteLine("Error: failed to clean " + item.Item1.Name + " - item is a directory");
        }
        catch (Exception e) when (e is IOException)
        {
          Console.WriteLine("Error: failed to clean " + item.Item1.Name + " - IOException");
        }
      }

      else if (Directory.Exists(trashFilesDir + "/" + item.Item2.Name))
      {
        try
        {
          Directory.Move(trashFilesDir + "/" + item.Item2.Name, item.Item2.OriginalPath);
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          Console.WriteLine("Error: failed to restore " + item.Item2.Name + " - insufficient permissions.");
          continue;
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException)
        {
          Console.WriteLine("Error: failed to restore " + item.Item2.Name + " - source or destination is null");
          continue;
        }
        catch (Exception e) when (e is DirectoryNotFoundException)
        {
          Console.WriteLine("Error: failed to restore " + item.Item2.Name + " - original location not found");
          continue;
        }
        catch (Exception e) when (e is PathTooLongException)
        {
          Console.WriteLine("Error: failed to restore " + item.Item2.Name +
                            " - source or destination path exceeds system maximum path length");
          continue;
        }
        catch (Exception e) when (e is IOException)
        {
          Console.WriteLine("Error: failed to restore " + item.Item2.Name + "(IOException)");
          continue;
        }
        catch (Exception e)
        {
          Console.WriteLine("Error: failed to restore " + item.Item2.Name + " - " + e);
          continue;
        }

        try
        {
          item.Item1.Delete();
        }
        catch (Exception e) when (e is SecurityException)
        {
          Console.WriteLine("Error: failed to clean " + item.Item1.Name + " - insufficient permissions");
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          Console.WriteLine("Error: failed to clean " + item.Item1.Name + " - item is a directory");
        }
        catch (Exception e) when (e is IOException)
        {
          Console.WriteLine("Error: failed to clean " + item.Item1.Name + " - IOException");
        }
      }
    }
  }

  public static List<FileDetails> GetTrashContents()
  {
    TestTrashDirectories();
    DirectoryInfo trashInfoDir = new DirectoryInfo(_trashLocation + "/info");
    DirectoryInfo trashFilesDir = new DirectoryInfo(_trashLocation + "/files");
    List<FileDetails> trashContents = new List<FileDetails>();
    FileInfo[] trashInfoFiles = trashInfoDir.GetFiles();
    if (trashInfoFiles.Length <= 0)
    {
      return trashContents;
    }

    foreach (FileInfo infoFile in trashInfoFiles)
    {
      FileStream stream = infoFile.Open(FileMode.Open);
      StreamReader reader = new StreamReader(stream);
      FileDetails fileDetails = new FileDetails();
      string? readString;
      while (!reader.EndOfStream)
      {
        readString = reader.ReadLine();
        if (readString == null || readString[0] == '[')
        {
          continue;
        }

        KeyValuePair<string, string> kvp =
          new KeyValuePair<string, string>(readString.Split('=')[0], readString.Split('=')[1]);

        switch (kvp.Key)
        {
          case "Path":
            fileDetails.OriginalPath = kvp.Value;
            fileDetails.Name = kvp.Value.Split('/')[^1];
            break;
          case "DeletionDate":
            string timeString = kvp.Value;
            timeString = timeString.Replace('-', '/');
            timeString = timeString.Replace('T', ' ');
            fileDetails.TimeDeleted = timeString;
            break;
        }
      }

      if (File.Exists(trashFilesDir + "/" + fileDetails.Name))
      {
        fileDetails.Size = HelperFunctions.ConvertBytes((new FileInfo(trashFilesDir.FullName + "/" + fileDetails.Name)).Length);
      }
      else if (Directory.Exists(trashFilesDir + "/" + fileDetails.Name))
      {
        FileSystemInfo[] contents = new DirectoryInfo(trashFilesDir + "/" + fileDetails.Name).GetFileSystemInfos("*", SearchOption.AllDirectories);
        long totalSize = 0;
        foreach (FileSystemInfo item in contents)
        {
          switch (item)
          {
            case FileInfo fileInfo:
              totalSize = totalSize + fileInfo.Length;
              break;
          }
        }

        fileDetails.Size = HelperFunctions.ConvertBytes(totalSize);
      }

      trashContents.Add(fileDetails);

    }

    return trashContents;
  }

  public static void EmptyTrashContents()
  {
    TestTrashDirectories();
    DirectoryInfo trashFilesDir = new DirectoryInfo(_trashLocation + "/files");
    DirectoryInfo trashInfoDir = new DirectoryInfo(_trashLocation + "/info");
    Console.WriteLine(trashFilesDir.GetFileSystemInfos().Length + " items in trash.");
    Console.WriteLine("Confirm deletion? Y/(N)");
    ConsoleKeyInfo confirmKey = Console.ReadKey(true);
    if (confirmKey.Key == ConsoleKey.Y)
    {
      foreach (FileSystemInfo item in trashFilesDir.EnumerateFileSystemInfos())
      {
        switch (item)
        {
          case FileInfo fileInfo:
            try
            {
              File.Delete(fileInfo.FullName);
              break;
            }
            catch (Exception e)
            {
              Console.WriteLine(e);
              break;
            }
          case DirectoryInfo directoryInfo:
            try
            {
              Directory.Delete(directoryInfo.FullName, true);
              break;
            }
            catch (Exception e)
            {
              Console.WriteLine(e);
              break;
            }
        }
      }
    }

    foreach (FileSystemInfo item in trashInfoDir.EnumerateFileSystemInfos())
    {
      try
      {
        File.Delete(item.FullName);
        break;
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        break;
      }
    }
  }

  public static void purgeFromTrash(string file)
  {
    TestTrashDirectories();
    if (file.Contains('/'))
    {
      Console.WriteLine("Invalid character in input: " + file);
      return;
    }

    TestTrashDirectories();
    DirectoryInfo trashInfoDir = new DirectoryInfo(_trashLocation + "/info");
    DirectoryInfo trashFilesDir = new DirectoryInfo(_trashLocation + "/files");
    List<Tuple<FileInfo,FileDetails>> trashContents = new List<Tuple<FileInfo, FileDetails>>();
    FileInfo[] trashInfoFiles = trashInfoDir.GetFiles();
    if (trashInfoFiles.Length <= 0)
    {
      return;
    }

    foreach (FileInfo infoFile in trashInfoFiles)
    {
      FileStream stream = infoFile.Open(FileMode.Open);
      StreamReader reader = new StreamReader(stream);
      FileDetails fileDetails = new FileDetails();
      string? readString;
      while (!reader.EndOfStream)
      {
        readString = reader.ReadLine();
        if (readString == null || readString[0] == '[')
        {
          continue;
        }

        KeyValuePair<string, string> kvp =
          new KeyValuePair<string, string>(readString.Split('=')[0], readString.Split('=')[1]);

        switch (kvp.Key)
        {
          case "Path":
            fileDetails.OriginalPath = kvp.Value;
            fileDetails.Name = kvp.Value.Split('/')[^1];
            break;
          case "DeletionDate":
            string timeString = kvp.Value;
            timeString = timeString.Replace('-', '/');
            timeString = timeString.Replace('T', ' ');
            fileDetails.TimeDeleted = timeString;
            break;
        }
      }

      if (File.Exists(trashFilesDir + "/" + fileDetails.Name))
      {
        fileDetails.Size = HelperFunctions.ConvertBytes((new FileInfo(trashFilesDir.FullName + "/" + fileDetails.Name)).Length);
      }
      else if (Directory.Exists(trashFilesDir + "/" + fileDetails.Name))
      {
        FileSystemInfo[] contents = new DirectoryInfo(trashFilesDir + "/" + fileDetails.Name).GetFileSystemInfos("*", SearchOption.AllDirectories);
        long totalSize = 0;
        foreach (FileSystemInfo item in contents)
        {
          switch (item)
          {
            case FileInfo fileInfo:
              totalSize = totalSize + fileInfo.Length;
              break;
          }
        }

        fileDetails.Size = HelperFunctions.ConvertBytes(totalSize);
      }
      Tuple<FileInfo, FileDetails> trashItem = new Tuple<FileInfo, FileDetails>(infoFile, fileDetails);
      trashContents.Add(trashItem);
    }
    string pattern = file.Replace(@".", @"\.");
    pattern = pattern.Replace("*", @".*");
    Regex starReplace = new Regex($"^{pattern}$");
    IEnumerable<Tuple<FileInfo,FileDetails>> searchResult = from item in (trashContents)
      where starReplace.IsMatch(item.Item2.Name)
      select item;

    foreach (Tuple<FileInfo, FileDetails> item in searchResult)
    {
      if (File.Exists(trashFilesDir + "/" + item.Item2.Name))
      {
        try
        {
          File.Delete(trashFilesDir + "/" + item.Item2.Name);
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException)
        {
          Console.WriteLine("Error: failed to delete " + item.Item2.Name + " - null argument");
          continue;
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          Console.WriteLine("Error: failed to delete " + item.Item2.Name + " - insufficient permissions.");
          continue;
        }
        catch (Exception e) when (e is PathTooLongException)
        {
          Console.WriteLine("Error: failed to delete " + item.Item2.Name +
                            " - path exceeds system maximum path length");
          continue;
        }
        catch (Exception e) when (e is DirectoryNotFoundException or NotSupportedException)
        {
          Console.WriteLine("Error: failed to delete " + item.Item2.Name + " - unable to locate file.");
          continue;
        }
        catch (Exception e) when (e is IOException)
        {
          Console.WriteLine("Error: failed to delete " + item.Item2.Name + "(IOException)");
          continue;
        }

        try
        {
          item.Item1.Delete();
        }
        catch (Exception e) when (e is SecurityException)
        {
          Console.WriteLine("Error: failed to clean " + item.Item1.Name + " - insufficient permissions");
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          Console.WriteLine("Error: failed to clean " + item.Item1.Name + " - item is a directory");
        }
        catch (Exception e) when (e is IOException)
        {
          Console.WriteLine("Error: failed to clean " + item.Item1.Name + " - IOException");
        }
      }

      else if (Directory.Exists(trashFilesDir + "/" + item.Item2.Name))
      {
        try
        {
          Directory.Delete(trashFilesDir + "/" + item.Item2.Name, true);
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          Console.WriteLine("Error: failed to delete " + item.Item2.Name + " - insufficient permissions.");
          continue;
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException)
        {
          Console.WriteLine("Error: failed to delete " + item.Item2.Name + " - source or destination is null");
          continue;
        }
        catch (Exception e) when (e is DirectoryNotFoundException)
        {
          Console.WriteLine("Error: failed to delete " + item.Item2.Name + " - unable to find directory");
          continue;
        }
        catch (Exception e) when (e is PathTooLongException)
        {
          Console.WriteLine("Error: failed to delete " + item.Item2.Name + " - path exceeds system maximum path length");
          continue;
        }
        catch (Exception e) when (e is IOException)
        {
          Console.WriteLine("Error: failed to delete " + item.Item2.Name + "(IOException)");
          continue;
        }

        try
        {
          item.Item1.Delete();
        }
        catch (Exception e) when (e is SecurityException)
        {
          Console.WriteLine("Error: failed to clean " + item.Item1.Name + " - insufficient permissions");
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          Console.WriteLine("Error: failed to clean " + item.Item1.Name + " - item is a directory");
        }
        catch (Exception e) when (e is IOException)
        {
          Console.WriteLine("Error: failed to clean " + item.Item1.Name + " - IOException");
        }
      }
    }
  }

}



