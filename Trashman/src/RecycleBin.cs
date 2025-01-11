using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using Shell32;

namespace Trashman;

// This class is only included on Windows so it will never be called on other platforms

public static partial class RecycleBin
{
  #region Enums

  [Flags]
  enum RecycleBinFlags : uint
  {
    SHERB_NOCONFIRMATION = 0x00000001,
    SHERB_NOPROGRESSUI = 0x00000002,
    SHERB_NOSOUND = 0x00000004
  }

  #endregion

  #region Shell32.dll Structs

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
  struct SHQUERYRBINFO
  {
    public int cbSize;
    public long i64Size;
    public long i64NumItems;
  }

  #endregion

  #region Shell32.dll Methods

  [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
  private static partial int SHQueryRecycleBinW(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

  [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
  private static partial int SHEmptyRecycleBinW(IntPtr hwnd, string pszRootPath, uint dwFlags);

  #endregion

  private static readonly string[] _protectedPaths = new[]
  {
    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
    Environment.GetFolderPath(Environment.SpecialFolder.System),
    Environment.GetFolderPath(Environment.SpecialFolder.SystemX86),
    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
    Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
    Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86),
    Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
    Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
  };

  public static void SendToRecycleBin(FileSystemInfo file)
  {
    if (_protectedPaths.Contains(file.FullName))
    {
      Console.WriteLine("Error: " + file.FullName + " is a protected system path");
      return;
    }

    switch (file)
    {
      case FileInfo fileInfo:
        try
        {
          FileSystem.DeleteFile(fileInfo.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
          break;
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException or NotSupportedException or PathTooLongException)
        {
          Console.Error.WriteLine("Path " + fileInfo.FullName + " does not exist or is an invalid path");
          break;
        }
        catch (Exception e) when (e is FileNotFoundException)
        {
          Console.Error.WriteLine("Not found: " + fileInfo.FullName);
          break;
        }
        catch (Exception e) when (e is SecurityException or UnauthorizedAccessException)
        {
          Console.Error.WriteLine("Permissions error, cannot delete " + fileInfo.FullName);
          break;
        }
        catch (Exception e) when (e is IOException)
        {
          Console.Error.WriteLine("Unable to delete " + fileInfo.FullName + " - file is in use by another process");
          break;
        }

      case DirectoryInfo directoryInfo:
        try
        {
          FileSystem.DeleteDirectory(directoryInfo.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
          break;
        }
        catch (Exception e) when (e is ArgumentException or ArgumentNullException or NotSupportedException or PathTooLongException)
        {
          Console.Error.WriteLine("Path " + directoryInfo.FullName + " does not exist or is an invalid path");
          break;
        }
        catch (Exception e) when (e is DirectoryNotFoundException)
        {
          Console.Error.WriteLine("Not found: " + directoryInfo.FullName);
          break;
        }
        catch (Exception e) when (e is SecurityException or UnauthorizedAccessException)
        {
          Console.Error.WriteLine("Insufficient permissions, cannot delete " + directoryInfo.FullName);
          break;
        }
        catch (Exception e) when (e is IOException)
        {
          Console.Error.WriteLine("Unable to delete " + directoryInfo.FullName + " - directory or subdirectory is in use by another process");
          break;
        }
        catch (Exception e)
        {
          Console.Error.WriteLine("Error: " + e);
          break;
        }
      default:
        Console.Error.WriteLine("Error: " + file.FullName + " is not a file or directory");
        break;
    }
  }

  public static void RestoreFromRecycleBin(List<string> file)
  {
    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
    {
      RestoreFromRecycleBinSTA(file);
    }
    else
    {
      Thread staThread = new Thread(
        () => { RestoreFromRecycleBinSTA(file); });
      staThread.SetApartmentState(ApartmentState.STA);
      staThread.Start();
      staThread.Join();
    }
  }

  private static void RestoreFromRecycleBinSTA(List<string> filesToRestore)
  {
    Shell shell = new Shell();
    Folder recycleBinFolder = shell.NameSpace(10);
    FolderItems recycleBinItems = recycleBinFolder.Items();
    Regex reservedCharacters = new Regex(@"[<>:|?""]+");

    foreach (string file in filesToRestore)
    {
      if (reservedCharacters.IsMatch(file))
      {
        Console.Error.WriteLine("Invalid character in input: " + file);
        continue;
      }

      string pattern = file.Replace(@".", @"\.");
      pattern = pattern.Replace("*", ".*");
      Regex starReplace = new Regex($"^{pattern}$");
      IEnumerable<FolderItem> searchResult = from item in (recycleBinItems.Cast<FolderItem>())
                                             where starReplace.IsMatch(item.Name)
                                             select item;
      foreach (FolderItem result in searchResult)
      {
        foreach (FolderItemVerb verb in result.Verbs())
        {
          if (verb.Name.Contains("Restore") || (verb.Name.Contains("R&estore")))
          {
            try
            {
              verb.DoIt();
            }
            catch (Exception e)
            {
              Console.Error.WriteLine("Failed to restore " + result.Name + ":" + e.Message);
            }
          }
        }
      }
    }
  }

  private static Tuple<long, long> GetRecycleBinContentInfo()
  {
    SHQUERYRBINFO recycleBinQueryInfo = new SHQUERYRBINFO();
    recycleBinQueryInfo.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));
    int queryHResult = SHQueryRecycleBinW(String.Empty, ref recycleBinQueryInfo);
    if (queryHResult != 0)
    {
      Console.Error.WriteLine("Error querying Recycle Bin contents. HRESULT: " + queryHResult);
      return new Tuple<long, long>(0, 0);
    }
    else
    {
      return new Tuple<long, long>(recycleBinQueryInfo.i64NumItems, recycleBinQueryInfo.i64Size);
    }
  }

  public static List<FileDetails> GetRecycleBinItems()
  {
    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
    {
      List<FileDetails> recycleBinItems = GetRecycleBinItemsSTA();
      return recycleBinItems;
    }
    else
    {
      List<FileDetails> recycleBinItems = new List<FileDetails>();
      Thread staThread = new Thread(
        () => { recycleBinItems = GetRecycleBinItemsSTA(); });
      staThread.SetApartmentState(ApartmentState.STA);
      staThread.Start();
      staThread.Join();
      return recycleBinItems;
    }
  }

  private static List<FileDetails> GetRecycleBinItemsSTA()
  {
    List<FileDetails> recycleBinItems = new List<FileDetails>();
    Shell shell = new Shell();
    Folder recycleBinFolder = shell.NameSpace(10);

    for (int i = 0; i < recycleBinFolder.Items().Count; i++)
    {
      FolderItem folderItem = recycleBinFolder.Items().Item(i);
      FileDetails fileDetails = new FileDetails(
        recycleBinFolder.GetDetailsOf(folderItem, 0),
        recycleBinFolder.GetDetailsOf(folderItem, 3).TrimStart().TrimEnd().Replace("bytes", "B"),
        recycleBinFolder.GetDetailsOf(folderItem, 1),
        (recycleBinFolder.GetDetailsOf(folderItem, 2)).Replace("?", "").TrimStart().TrimEnd()
      );
      recycleBinItems.Add(fileDetails);
    }

    return recycleBinItems;
  }

  public static void EmptyRecycleBinContents()
  {
    uint flags = (uint)(RecycleBinFlags.SHERB_NOCONFIRMATION | RecycleBinFlags.SHERB_NOPROGRESSUI |
                        RecycleBinFlags.SHERB_NOSOUND);
    Tuple<long, long> recycleBinContentInfo = GetRecycleBinContentInfo();
    if (recycleBinContentInfo.Item1 != 0)
    {
      Console.WriteLine(recycleBinContentInfo.Item1 + " items found in Recycle Bin (" + HelperFunctions.ConvertBytes(recycleBinContentInfo.Item2) + ")");
      Console.WriteLine("Confirm deletion? Y/(N)");
      ConsoleKeyInfo confirmKey = Console.ReadKey(true);
      if (confirmKey.Key == ConsoleKey.Y)
      {
        Console.WriteLine("Deleting...");
        int hResult = SHEmptyRecycleBinW(IntPtr.Zero, String.Empty, flags);
        if (hResult == 0)
        {
          Console.WriteLine("Recycle Bin emptied.");
        }
        else
        {
          Console.Error.WriteLine("Error emptying Recycle Bin (HRESULT: " + hResult);
        }
      }
      else
      {
        Console.WriteLine("Recycle Bin not emptied.");
      }
    }
  }

  public static void PurgeFromRecycleBin(List<string> file)
  {
    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
    {
      PurgeFromRecycleBinSTA(file);
    }
    else
    {
      Thread staThread = new Thread(
        () => { PurgeFromRecycleBinSTA(file); });
      staThread.SetApartmentState(ApartmentState.STA);
      staThread.Start();
      staThread.Join();
    }
  }

  private static void PurgeFromRecycleBinSTA(List<string> filesToPurge)
  {
    Regex reservedCharacters = new Regex(@"[<>:|?""]+");
    Shell shell = new Shell();
    Folder recycleBinFolder = shell.NameSpace(10);
    FolderItems recycleBinItems = recycleBinFolder.Items();

    foreach (string file in filesToPurge)
    {
      if (reservedCharacters.IsMatch(file))
      {
        Console.Error.WriteLine("Invalid character in input" + file);
        continue;
      }

      string pattern = file.Replace(@"\", @"\\");
      pattern = pattern.Replace(@".", @"\.");
      pattern = pattern.Replace("*", ".*");
      Regex starReplace = new Regex($"^(?i){pattern}$");
      IEnumerable<FolderItem> searchResult = from item in (recycleBinItems.Cast<FolderItem>())
                                             where starReplace.IsMatch(item.Name)
                                             select item;
      if (!searchResult.Any())
      {
        Console.WriteLine("No results for " + file + " in Recycle Bin.");
        continue;
      }

      foreach (FolderItem item in searchResult)
      {
        if (item.IsFileSystem == false)
        {
          Console.Error.WriteLine("Error: " + item.Name + " is not a normal filesystem object. Ignoring.");
          continue;
        }

        if (item.IsFolder)
        {
          try
          {
            if (item.Type == "Compressed (zipped) Folder")
            {
              File.Delete(item.Path);
            }
            else
            {
              Directory.Delete(item.Path, true);
            }
          }
          catch (Exception e) when (e is ArgumentNullException or ArgumentException)
          {
            Console.Error.WriteLine("Error: path is null or empty, or includes invalid characters");
          }
          catch (Exception e) when (e is IOException)
          {
            Console.Error.WriteLine("Error: " + item.Name + " cannot be deleted, is it in use?");
          }
          catch (Exception e) when (e is PathTooLongException)
          {
            Console.Error.WriteLine("Error: " + item.Path + " is invalid or exceeds the maximum path length");
          }
          catch (Exception e) when (e is UnauthorizedAccessException)
          {
            Console.Error.WriteLine("Error: Permissions error, cannot delete " + item.Name);
          }
          catch (Exception e) when (e is DirectoryNotFoundException)
          {
            Console.Error.WriteLine("Error: " + item.Path + " was not found");
          }
        }
        else
        {
          try
          {
            File.Delete(item.Path);
          }
          catch (Exception e) when (e is ArgumentNullException or ArgumentException)
          {
            Console.Error.WriteLine("Error: path is null or empty, or includes invalid characters");
          }
          catch (Exception e) when (e is IOException)
          {
            Console.Error.WriteLine("Error: " + item.Name + " cannot be deleted, is it open?");
          }
          catch (Exception e) when (e is NotSupportedException or PathTooLongException)
          {
            Console.Error.WriteLine("Error: " + item.Path + " is invalid or exceeds the maximum path length");
          }
          catch (Exception e) when (e is UnauthorizedAccessException)
          {
            Console.Error.WriteLine("Error: Permissions error, cannot delete " + item.Name);
          }
        }
      }
    }
  }
}