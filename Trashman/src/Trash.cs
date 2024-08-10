using System.Security;
using System.Text.RegularExpressions;

namespace Trashman;

public class Trash
{
  private static string _trashLocation = (Environment.GetEnvironmentVariable("XDG_DATA_HOME") == (String.Empty) || Environment.GetEnvironmentVariable("XDG_DATA_HOME") == null)
      ? Environment.GetEnvironmentVariable("HOME") + "/.local/share/Trash"
      : Environment.GetEnvironmentVariable("XDG_DATA_HOME") + "/Trash";

  private static readonly string[] _protectedPaths = new[]
  {
    "/",
    "/bin",
    "/boot",
    "/dev",
    "/etc",
    "/home",
    "/lib",
    "/lib64",
    "/lost+found",
    "/media",
    "/mnt",
    "/opt",
    "/proc",
    "/root",
    "/run",
    "/sbin",
    "/srv",
    "/sys",
    "/tmp",
    "/usr",
    "/var",
    "/nix"
  };

  private static void TestTrashDirectories()
  {
    if (!Directory.Exists(_trashLocation))
    {
      try
      {
        Directory.CreateDirectory(_trashLocation, (UnixFileMode.UserRead | UnixFileMode.UserWrite));
      }
      catch (Exception e)
      {
        Console.Error.WriteLine("Trash directory does not exist and cannot be created - " + e.GetType());
        return;
      }
    }

    if (!Directory.Exists(_trashLocation + "/files"))
    {
      try
      {
        Directory.CreateDirectory(_trashLocation + "/files");
      }
      catch (Exception e)
      {
        Console.Error.WriteLine("Trash/files directory does not exist and cannot be created - " + e.GetType());
        return;
      }
    }

    if (!Directory.Exists(_trashLocation + "/info"))
    {
      try
      {
        Directory.CreateDirectory(_trashLocation + "/info");
      }
      catch (Exception e)
      {
        Console.Error.WriteLine("Trash/info directory does not exist and cannot be created - " + e.GetType());
        return;
      }
    }
  }

  public static void SendToTrash(FileSystemInfo file)
  {
    if (_protectedPaths.Contains(file.FullName))
    {
      Console.Error.WriteLine("Error: cannot trash protected system files.");
      return;
    }

    TestTrashDirectories();

    string trashFileName;
    FileStream fileStream;
    StreamWriter writer;
    bool errorEncountered = false;

    switch (file)
    {
      case FileInfo fileInfo:
        trashFileName = fileInfo.Name;
        if (File.Exists(_trashLocation + "/files/" + trashFileName))
        {
          int counter = 0;
          while (File.Exists(_trashLocation + "/files/" + trashFileName))
          {
            trashFileName = trashFileName + counter.ToString();
          }
        }

        try
        {
          fileStream = File.Create(_trashLocation + "/info/" + trashFileName + ".trashinfo");
          writer = new StreamWriter(fileStream);
          writer.AutoFlush = true;
          writer.Write("[Trash Info]" + Environment.NewLine);
          writer.Write("Path=" + fileInfo.FullName + Environment.NewLine);
          writer.Write("DeletionDate=" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + Environment.NewLine);
          writer.Close();
          fileStream.Close();
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          Console.Error.WriteLine("Failed to create " + trashFileName +".trashinfo file - insufficient permissions.");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException or NotSupportedException)
        {
          Console.Error.WriteLine("Failed to create " + trashFileName +".trashinfo file - path is null or invalid");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is PathTooLongException)
        {
          Console.Error.WriteLine("Failed to create " + trashFileName +".trashinfo file - path exceeds system maximum path length");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is DirectoryNotFoundException)
        {
          Console.Error.WriteLine("Failed to create " + trashFileName +".trashinfo file - directory not found");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is IOException)
        {
          Console.Error.WriteLine("Failed to create " + trashFileName +".trashinfo file - IOException");
          errorEncountered = true;
          return;
        }
        finally
        {
          if (errorEncountered)
          {
            try
            {
              if (File.Exists(_trashLocation + "/info/" + trashFileName + ".trashinfo"))
              {
                File.Delete(_trashLocation + "/info/" + trashFileName + ".trashinfo");
              }
            }
            catch (Exception e)
            {
              Console.Error.WriteLine("Encountered an error while attempting to clean up from another exception. Extraneous trashinfo file may exist. Error: " + e);
            }
            errorEncountered = false;
          }
        }

        try
        {
          File.Move(fileInfo.FullName, _trashLocation + "/files/" + fileInfo.Name);
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException or NotSupportedException)
        {
          Console.Error.WriteLine("Failed to move " + fileInfo.FullName + " to trash - source or destination is null or invalid");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is IOException)
        {
          Console.Error.WriteLine("Failed to move " + fileInfo.FullName + " to trash - IOException");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is FileNotFoundException)
        {
          Console.Error.WriteLine("Failed to move " + fileInfo.FullName + " to trash - file not found");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          Console.Error.WriteLine("Failed to move " + fileInfo.FullName + " to trash - insufficient permissions");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is PathTooLongException)
        {
          Console.Error.WriteLine("Failed to move " + fileInfo.FullName + " to trash - path exceeds system maximum path length");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is DirectoryNotFoundException)
        {
          Console.Error.WriteLine("Failed to move " + fileInfo.FullName + " to trash - directory not found");
          errorEncountered = true;
          return;
        }
        finally
        {
          if (errorEncountered)
          {
            if (File.Exists(_trashLocation + "/info/" + trashFileName + ".trashinfo"))
            {
              try
              {
                File.Delete(_trashLocation + "/info/" + trashFileName + ".trashinfo");
              }
              catch (Exception e)
              {
                Console.Error.WriteLine("Encountered an error while attempting to clean up from another exception. Extraneous trashinfo file may exist. Error: " + e);
              }
            }
            errorEncountered = false;
          }
        }

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

        try
        {
          fileStream = File.Create(_trashLocation + "/info/" + trashFileName + ".trashinfo");
          writer = new StreamWriter(fileStream);
          writer.Write("[Trash Info]" + Environment.NewLine);
          writer.Write("Path=" + directoryInfo.FullName + Environment.NewLine);
          writer.Write("DeletionDate=" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + Environment.NewLine);
          writer.Close();
          fileStream.Close();
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          Console.Error.WriteLine("Failed to create " + trashFileName + ".trashinfo file - insufficient permissions.");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException or NotSupportedException)
        {
          Console.Error.WriteLine("Failed to create " + trashFileName + ".trashinfo file - path is null or invalid");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is PathTooLongException)
        {
          Console.Error.WriteLine("Failed to create " + trashFileName + ".trashinfo file - path exceeds system maximum path length");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is DirectoryNotFoundException)
        {
          Console.Error.WriteLine("Failed to create " + trashFileName + ".trashinfo file - directory not found");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is IOException)
        {
          Console.Error.WriteLine("Failed to create " + trashFileName + ".trashinfo file - IOException");
          errorEncountered = true;
          return;
        }
        finally
        {
          if (errorEncountered)
          {
            if (File.Exists(_trashLocation + "/info/" + trashFileName + ".trashinfo"))
            {
              try
              {
                File.Delete(_trashLocation + "/info/" + trashFileName + ".trashinfo");
              }
              catch (Exception e)
              {
                Console.Error.WriteLine("Encountered an error while attempting to clean up from another exception. Extraneous trashinfo file may exist. Error: " + e);
              }
            }
            errorEncountered = false;
          }
        }

        try
        {
          Directory.Move(directoryInfo.FullName, _trashLocation + "/files/" + directoryInfo.Name);
        }
        catch (Exception e) when (e is FileNotFoundException)
        {
          Console.Error.WriteLine("Failed to move " + directoryInfo.FullName + " directory to trash - directory not found");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          Console.Error.WriteLine("Failed to move " + directoryInfo.FullName + " directory to trash - insufficient permissions");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException or NotSupportedException)
        {
          Console.Error.WriteLine("Failed to move " + directoryInfo.FullName + " directory to trash - source or destination is null or invalid");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is PathTooLongException)
        {
          Console.Error.WriteLine("Failed to move " + directoryInfo.FullName + " directory to trash - path exceeds system maximum path length");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is DirectoryNotFoundException)
        {
          Console.Error.WriteLine("Failed to move " + directoryInfo.FullName + " directory to trash - directory not found");
          errorEncountered = true;
          return;
        }
        catch (Exception e) when (e is IOException)
        {
          Console.Error.WriteLine("Failed to move " + directoryInfo.FullName + " directory to trash - IOException");
          errorEncountered = true;
          return;
        }
        finally
        {
          if (errorEncountered)
          {
            if (File.Exists(_trashLocation + "/info/" + trashFileName + ".trashinfo"))
            {
              try
              {
                File.Delete(_trashLocation + "/info/" + trashFileName + ".trashinfo");
              }
              catch (Exception e)
              {
                Console.Error.WriteLine("Encountered an error while attempting to clean up from another exception. Extraneous trashinfo file may exist. Error: " + e);
              }
            }
            errorEncountered = false;
          }
        }

        break;
    }
  }

  public static void RestoreFromTrash(List<string> fileList)
  {
    TestTrashDirectories();

    DirectoryInfo trashInfoDir;
    DirectoryInfo trashFilesDir;

    try
    {
      trashInfoDir = new DirectoryInfo(_trashLocation + "/info");
      trashFilesDir = new DirectoryInfo(_trashLocation + "/files");
    }
    catch (Exception e) when (e is ArgumentException or ArgumentNullException)
    {
      Console.Error.WriteLine("Unable to obtain handle of trash directories - path is null or invalid");
      return;
    }
    catch (Exception e) when (e is SecurityException)
    {
      Console.Error.WriteLine("Unable to obtain handle of trash directories - insufficient permissions");
      return;
    }
    catch (Exception e) when (e is PathTooLongException)
    {
      Console.Error.WriteLine("Unable to obtain handle of trash directories - path exceeds system maximum path length");
      return;
    }

    List<Tuple<FileInfo, FileDetails>> trashContents = new List<Tuple<FileInfo, FileDetails>>();
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
      while (!reader.EndOfStream)
      {
        string? readString = reader.ReadLine();
        if (readString == null || readString[0] == '[')
        {
          continue;
        }

        KeyValuePair<string, string> kvp = new KeyValuePair<string, string>(readString.Split('=')[0], readString.Split('=')[1]);

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

    foreach (string file in fileList)
    {
      if (file.Contains('/') || file.Contains("**"))
      {
        Console.Error.WriteLine("Invalid character in input: " + file);
        continue;
      }

      string pattern = file.Replace(@".", @"\.");
      pattern = pattern.Replace("*", @".*");
      Regex starReplace = new Regex($"^{pattern}$");

      IEnumerable<Tuple<FileInfo, FileDetails>> searchResult = from item in (trashContents)
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
            Console.Error.WriteLine("Error: failed to restore " + item.Item2.Name + " - source or destination is null");
            continue;
          }
          catch (Exception e) when (e is UnauthorizedAccessException)
          {
            Console.Error.WriteLine("Error: failed to restore " + item.Item2.Name + " - insufficient permissions.");
            continue;
          }
          catch (Exception e) when (e is PathTooLongException)
          {
            Console.Error.WriteLine("Error: failed to restore " + item.Item2.Name + " - source or destination path exceeds system maximum path length");
            continue;
          }
          catch (Exception e) when (e is FileNotFoundException)
          {
            Console.Error.WriteLine("Error: failed to restore " + item.Item2.Name + " - unable to locate file.");
            continue;
          }
          catch (Exception e) when (e is DirectoryNotFoundException or NotSupportedException)
          {
            Console.Error.WriteLine("Error: failed to restore " + item.Item2.Name + " - target or destination is invalid");
            continue;
          }
          catch (Exception e) when (e is IOException)
          {
            Console.Error.WriteLine("Error: failed to restore " + item.Item2.Name + "(IOException)");
            continue;
          }
          catch (Exception e)
          {
            Console.Error.WriteLine("Error: failed to restore " + item.Item2.Name + " - " + e);
            continue;
          }

          try
          {
            item.Item1.Delete();
          }
          catch (Exception e) when (e is SecurityException)
          {
            Console.Error.WriteLine("Error: failed to clean " + item.Item1.Name + " - insufficient permissions");
          }
          catch (Exception e) when (e is UnauthorizedAccessException)
          {
            Console.Error.WriteLine("Error: failed to clean " + item.Item1.Name + " - item is a directory");
          }
          catch (Exception e) when (e is IOException)
          {
            Console.Error.WriteLine("Error: failed to clean " + item.Item1.Name + " - IOException");
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
            Console.Error.WriteLine("Error: failed to restore " + item.Item2.Name + " - insufficient permissions.");
            continue;
          }
          catch (Exception e) when (e is ArgumentException or ArgumentNullException)
          {
            Console.Error.WriteLine("Error: failed to restore " + item.Item2.Name + " - source or destination is null");
            continue;
          }
          catch (Exception e) when (e is DirectoryNotFoundException)
          {
            Console.Error.WriteLine("Error: failed to restore " + item.Item2.Name + " - original location not found");
            continue;
          }
          catch (Exception e) when (e is PathTooLongException)
          {
            Console.Error.WriteLine("Error: failed to restore " + item.Item2.Name + " - source or destination path exceeds system maximum path length");
            continue;
          }
          catch (Exception e) when (e is IOException)
          {
            Console.Error.WriteLine("Error: failed to restore " + item.Item2.Name + "(IOException)");
            continue;
          }
          catch (Exception e)
          {
            Console.Error.WriteLine("Error: failed to restore " + item.Item2.Name + " - " + e);
            continue;
          }

          try
          {
            item.Item1.Delete();
          }
          catch (Exception e) when (e is SecurityException)
          {
            Console.Error.WriteLine("Error: failed to clean " + item.Item1.Name + " - insufficient permissions");
          }
          catch (Exception e) when (e is UnauthorizedAccessException)
          {
            Console.Error.WriteLine("Error: failed to clean " + item.Item1.Name + " - item is a directory");
          }
          catch (Exception e) when (e is IOException)
          {
            Console.Error.WriteLine("Error: failed to clean " + item.Item1.Name + " - IOException");
          }
        }
      }
    }
  }

  public static List<FileDetails> GetTrashContents()
  {
    TestTrashDirectories();

    DirectoryInfo trashInfoDir;
    DirectoryInfo trashFilesDir;

    try
    {
      trashInfoDir = new DirectoryInfo(_trashLocation + "/info");
      trashFilesDir = new DirectoryInfo(_trashLocation + "/files");
    }
    catch (Exception e) when (e is ArgumentException or ArgumentNullException)
    {
      Console.Error.WriteLine("Unable to obtain handle of trash directories - path is null or invalid");
      return null;
    }
    catch (Exception e) when (e is SecurityException)
    {
      Console.Error.WriteLine("Unable to obtain handle of trash directories - insufficient permissions");
      return null;
    }
    catch (Exception e) when (e is PathTooLongException)
    {
      Console.Error.WriteLine("Unable to obtain handle of trash directories - path exceeds system maximum path length");
      return null;
    }

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
      while (!reader.EndOfStream)
      {
        string? readString = reader.ReadLine();
        if (readString == null || readString[0] == '[')
        {
          continue;
        }

        KeyValuePair<string, string> kvp = new KeyValuePair<string, string>(readString.Split('=')[0], readString.Split('=')[1]);

        switch (kvp.Key)
        {
          case "Path":
            fileDetails.OriginalPath = kvp.Value;
            fileDetails.Name = kvp.Value.TrimEnd('/').Split('/')[^1];
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
        DirectoryInfo dir = new DirectoryInfo(trashFilesDir + "/" + fileDetails.Name);
        long totalSize = HelperFunctions.GetTotalDirectorySize(dir);
        fileDetails.Size = HelperFunctions.ConvertBytes(totalSize);
      }

      trashContents.Add(fileDetails);
    }

    return trashContents;
  }

  public static void EmptyTrashContents()
  {
    TestTrashDirectories();
    DirectoryInfo trashInfoDir;
    DirectoryInfo trashFilesDir;

    try
    {
      trashInfoDir = new DirectoryInfo(_trashLocation + "/info");
      trashFilesDir = new DirectoryInfo(_trashLocation + "/files");
    }
    catch (Exception e) when (e is ArgumentException or ArgumentNullException)
    {
      Console.Error.WriteLine("Unable to obtain handle of trash directories - path is null or invalid");
      return;
    }
    catch (Exception e) when (e is SecurityException)
    {
      Console.Error.WriteLine("Unable to obtain handle of trash directories - insufficient permissions");
      return;
    }
    catch (Exception e) when (e is PathTooLongException)
    {
      Console.Error.WriteLine("Unable to obtain handle of trash directories - path exceeds system maximum path length");
      return;
    }

    Console.WriteLine(trashFilesDir.GetFileSystemInfos().Length + " items in trash.");
    Console.WriteLine("Confirm deletion? Y/(N)");
    ConsoleKeyInfo confirmKey = Console.ReadKey(true);
    if (confirmKey.Key == ConsoleKey.Y)
    {
      Console.WriteLine("Deleting...")
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
            catch (Exception e) when (e is ArgumentException or ArgumentNullException or NotSupportedException or DirectoryNotFoundException)
            {
              Console.Error.WriteLine("Failed to delete " + fileInfo.FullName + " - path is null or invalid");
              break;
            }
            catch (Exception e) when (e is UnauthorizedAccessException)
            {
              Console.Error.WriteLine("Failed to delete " + fileInfo.FullName + " - insufficient permissions");
              break;
            }
            catch (Exception e) when (e is PathTooLongException)
            {
              Console.Error.WriteLine("Failed to delete " + fileInfo.FullName + " - path exceeds system maximum path length");
              break;
            }
            catch (Exception e) when (e is IOException)
            {
              Console.Error.WriteLine("Failed to delete " + fileInfo.FullName + " - file is in use");
              break;
            }

          case DirectoryInfo directoryInfo:
            try
            {
              Directory.Delete(directoryInfo.FullName, true);
              break;
            }
            catch (Exception e) when (e is IOException)
            {
              Console.Error.WriteLine("Failed to delete directory " + directoryInfo.FullName + " - directory is in use");
              break;
            }
            catch (Exception e) when (e is UnauthorizedAccessException)
            {
              Console.Error.WriteLine("Failed to delete directory " + directoryInfo.FullName + " - insufficient permissions");
              break;
            }
            catch (Exception e) when (e is ArgumentException or ArgumentNullException or NotSupportedException)
            {
              Console.Error.WriteLine("Failed to delete directory " + directoryInfo.FullName + " - path is null or invalid");
              break;
            }
            catch (Exception e) when (e is PathTooLongException)
            {
              Console.Error.WriteLine("Failed to delete directory " + directoryInfo.FullName + " - path exceeds system maximum path length");
              break;
            }
            catch (Exception e) when (e is DirectoryNotFoundException)
            {
              Console.Error.WriteLine("Failed to delete directory " + directoryInfo.FullName + " - directory not found");
              break;
            }
        }
      }

      foreach (FileSystemInfo item in trashInfoDir.EnumerateFileSystemInfos())
      {
        try
        {
          File.Delete(item.FullName);
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException or NotSupportedException or DirectoryNotFoundException)
        {
          Console.Error.WriteLine("Failed to delete " + item.FullName + " - path is null or invalid");
          break;
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
          Console.Error.WriteLine("Failed to delete " + item.FullName + " - insufficient permissions");
          break;
        }
        catch (Exception e) when (e is PathTooLongException)
        {
          Console.Error.WriteLine("Failed to delete " + item.FullName + " - path exceeds system maximum path length");
          break;
        }
        catch (Exception e) when (e is IOException)
        {
          Console.Error.WriteLine("Failed to delete " + item.FullName + " - file is in use");
          break;
        }
      }
    }
  }

  public static void PurgeFromTrash(List<string> fileList)
  {
    TestTrashDirectories();

    DirectoryInfo trashInfoDir;
    DirectoryInfo trashFilesDir;

    try
    {
      trashInfoDir = new DirectoryInfo(_trashLocation + "/info");
      trashFilesDir = new DirectoryInfo(_trashLocation + "/files");
    }
    catch (Exception e) when (e is ArgumentException or ArgumentNullException)
    {
      Console.Error.WriteLine("Unable to obtain handle of trash directories - path is null or invalid");
      return;
    }
    catch (Exception e) when (e is SecurityException)
    {
      Console.Error.WriteLine("Unable to obtain handle of trash directories - insufficient permissions");
      return;
    }
    catch (Exception e) when (e is PathTooLongException)
    {
      Console.Error.WriteLine("Unable to obtain handle of trash directories - path exceeds system maximum path length");
      return;
    }

    List<Tuple<FileInfo, FileDetails>> trashContents = new List<Tuple<FileInfo, FileDetails>>();
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
      while (!reader.EndOfStream)
      {
        string? readString = reader.ReadLine();
        if (readString == null || readString[0] == '[')
        {
          continue;
        }

        KeyValuePair<string, string> kvp = new KeyValuePair<string, string>(readString.Split('=')[0], readString.Split('=')[1]);

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

    foreach (string file in fileList)
    {
      if (file.Contains('/') || file.Contains("**"))
      {
        Console.Error.WriteLine("Invalid character in input: " + file);
        continue;
      }

      string pattern = file.Replace(@".", @"\.");
      pattern = pattern.Replace("*", @".*");
      Regex starReplace = new Regex($"^{pattern}$");
      IEnumerable<Tuple<FileInfo, FileDetails>> searchResult = from item in (trashContents)
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
            Console.Error.WriteLine("Error: failed to delete " + item.Item2.Name + " - null argument");
            continue;
          }
          catch (Exception e) when (e is UnauthorizedAccessException)
          {
            Console.Error.WriteLine("Error: failed to delete " + item.Item2.Name + " - insufficient permissions.");
            continue;
          }
          catch (Exception e) when (e is PathTooLongException)
          {
            Console.Error.WriteLine("Error: failed to delete " + item.Item2.Name + " - path exceeds system maximum path length");
            continue;
          }
          catch (Exception e) when (e is DirectoryNotFoundException or NotSupportedException)
          {
            Console.Error.WriteLine("Error: failed to delete " + item.Item2.Name + " - unable to locate file.");
            continue;
          }
          catch (Exception e) when (e is IOException)
          {
            Console.Error.WriteLine("Error: failed to delete " + item.Item2.Name + "(IOException)");
            continue;
          }

          try
          {
            item.Item1.Delete();
          }
          catch (Exception e) when (e is SecurityException)
          {
            Console.Error.WriteLine("Error: failed to clean " + item.Item1.Name + " - insufficient permissions");
          }
          catch (Exception e) when (e is UnauthorizedAccessException)
          {
            Console.Error.WriteLine("Error: failed to clean " + item.Item1.Name + " - item is a directory");
          }
          catch (Exception e) when (e is IOException)
          {
            Console.Error.WriteLine("Error: failed to clean " + item.Item1.Name + " - IOException");
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
            Console.Error.WriteLine("Error: failed to delete " + item.Item2.Name + " - insufficient permissions.");
            continue;
          }
          catch (Exception e) when (e is ArgumentException or ArgumentNullException)
          {
            Console.Error.WriteLine("Error: failed to delete " + item.Item2.Name + " - source or destination is null");
            continue;
          }
          catch (Exception e) when (e is DirectoryNotFoundException)
          {
            Console.Error.WriteLine("Error: failed to delete " + item.Item2.Name + " - unable to find directory");
            continue;
          }
          catch (Exception e) when (e is PathTooLongException)
          {
            Console.Error.WriteLine("Error: failed to delete " + item.Item2.Name + " - path exceeds system maximum path length");
            continue;
          }
          catch (Exception e) when (e is IOException)
          {
            Console.Error.WriteLine("Error: failed to delete " + item.Item2.Name + "(IOException)");
            continue;
          }

          try
          {
            item.Item1.Delete();
          }
          catch (Exception e) when (e is SecurityException)
          {
            Console.Error.WriteLine("Error: failed to clean " + item.Item1.Name + " - insufficient permissions");
            continue;
          }
          catch (Exception e) when (e is UnauthorizedAccessException)
          {
            Console.Error.WriteLine("Error: failed to clean " + item.Item1.Name + " - item is a directory");
            continue;
          }
          catch (Exception e) when (e is IOException)
          {
            Console.Error.WriteLine("Error: failed to clean " + item.Item1.Name + " - IOException");
            continue;
          }
        }
      }
    }
  }
}