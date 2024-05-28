using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using System.Security;
using Shell32;

namespace Trasher;

public class RecycleBin
{
  #region Enums

  [Flags]
  enum RecycleBinFlags: uint
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

  [DllImport("shell32.dll")]
  static extern int SHQueryRecycleBinW(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

  [DllImport("shell32.dll")]
  static extern int SHEmptyRecycleBinW(IntPtr hwnd, string pszRootPath, uint dwFlags);

  #endregion



  public static void SendToRecycleBin(FileSystemInfo file)
  {
    switch (file)
    {
      case FileInfo fileInfo:
        try
        {
          FileSystem.DeleteFile(fileInfo.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
          break;
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
          Console.WriteLine("Not found: " + fileInfo.FullName);
          break;
        }
        catch (Exception e) when (e is SecurityException or UnauthorizedAccessException)
        {
          Console.WriteLine("Permissions error, cannot delete " + fileInfo.FullName);
          break;
        }
        catch (Exception e)
        {
          Console.WriteLine("Error: " + e);
          break;
        }
      case DirectoryInfo directoryInfo:
        try
        {
          FileSystem.DeleteDirectory(directoryInfo.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
          break;
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
          Console.WriteLine("Not found: " + directoryInfo.FullName);
          break;
        }
        catch (Exception e) when (e is SecurityException or UnauthorizedAccessException)
        {
          Console.WriteLine("Insufficient permissions, cannot delete " + directoryInfo.FullName);
          break;
        }
        catch (Exception e)
        {
          Console.WriteLine("Error: " + e);
          break;
        }
      default:
        Console.WriteLine("Error: " + file.FullName + " is not a file or directory");
        break;
    }
  }

  public static void RestoreFromRecycleBin(string file)
  {
    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
    {
      Shell shell = new Shell();
      Folder recycleBinFolder = shell.NameSpace(10);
      FolderItems recycleBinItems = recycleBinFolder.Items();
      IEnumerable<FolderItem> collection = recycleBinItems.Cast<FolderItem>();
      IEnumerable<FolderItem> query = from item in collection
        where item.Name.Contains(file)
        select item;
      if (query.Count() == 1)
      {
        Console.WriteLine("Restoring...");
        foreach (FolderItem item in query)
        {
          foreach (FolderItemVerb verb in item.Verbs())
          {
            if (verb.Name.Contains("Restore"))
            {
              verb.DoIt();
            }
          }
        }
      }
      else
      {
        Console.WriteLine("Multiple matches detected, refine search");
      }
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

  private static void RestoreFromRecycleBinSTA(string file)
  {
    Shell shell = new Shell();
    Folder recycleBinFolder = shell.NameSpace(10);
    FolderItems recycleBinItems = recycleBinFolder.Items();
    IEnumerable<FolderItem> collection = recycleBinItems.Cast<FolderItem>();
    IEnumerable<FolderItem> query = from item in collection
      where item.Name.Contains(file)
      select item;
    if (query.Count() == 1)
    {
      Console.WriteLine("Restoring...");
      foreach (FolderItem item in query)
      {
        foreach (FolderItemVerb verb in item.Verbs())
        {
          if (verb.Name.Contains("Restore") || (verb.Name.Contains("R&estore")))
          {
            verb.DoIt();
          }
        }
      }
    }
    else
    {
      Console.WriteLine("Multiple matches detected, refine search");
    }
  }



  public static Tuple<long,long> GetRecycleBinContentInfo()
  {
    SHQUERYRBINFO recycleBinQueryInfo = new SHQUERYRBINFO();
    recycleBinQueryInfo.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));
    int queryHResult = SHQueryRecycleBinW(String.Empty, ref recycleBinQueryInfo);
    if (queryHResult != 0)
    {
      Console.WriteLine("Error querying Recycle Bin contents. HRESULT: " + queryHResult);
      throw new Exception("Error querying Recycle Bin contents. HRESULT: " + queryHResult);
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
      List<FileDetails> recycleBinItems = new List<FileDetails>();
      Shell shell = new Shell();
      Folder recycleBinFolder = shell.NameSpace(10);

      if (recycleBinFolder.Items().Count < 1)
      {
        return recycleBinItems;
      }
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
    Tuple<long,long> recycleBinContentInfo = GetRecycleBinContentInfo();
    if (recycleBinContentInfo.Item1 != 0)
    {
      Console.WriteLine(recycleBinContentInfo.Item1 + " items found in Recycle Bin (" + HelperFunctions.ConvertBytes(recycleBinContentInfo.Item2) + ")");
      Console.WriteLine("Confirm deletion? Y/(N)");
      ConsoleKeyInfo confirmKey = Console.ReadKey(true);
      if (confirmKey.Key == ConsoleKey.Y)
      {
        int hResult = SHEmptyRecycleBinW(IntPtr.Zero, String.Empty, flags);
        if (hResult == 0)
        {
          Console.WriteLine("Recycle Bin emptied.");
        }
        else
        {
          Console.WriteLine("Error emptying Recycle Bin (HRESULT: " + hResult);
        }
      }
      else
      {
        Console.WriteLine("Recycle Bin not emptied.");
      }
    }
  }


  public static void PurgeFromRecycleBin(string file)
  {
    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
    {
      Shell shell = new Shell();
      Folder recycleBinFolder = shell.NameSpace(10);
      FolderItems recycleBinItems = recycleBinFolder.Items();
      IEnumerable<FolderItem> collection = recycleBinItems.Cast<FolderItem>();
      IEnumerable<FolderItem> query = from item in collection
        where item.Name.Contains(file)
        select item;

        Console.WriteLine("Purging...");
        foreach (FolderItem item in query)
        {
          if (item.IsFileSystem == false)
          {
            Console.WriteLine("File isFileSystem false, what do?"); // TODO
            continue;
          }
          else if (item.IsFolder == true)
          {
            try
            {
              Directory.Delete(item.Path, true);
            }
            catch (Exception e) when (e is ArgumentNullException or ArgumentException)
            {
              Console.WriteLine("Error: path is null or empty, or includes invalid characters");
              continue;
            }
            catch (Exception e) when (e is IOException)
            {
              Console.WriteLine("Error: " + item.Name + " cannot be deleted, is it in use?");
              continue;
            }
            catch (Exception e) when (e is PathTooLongException)
            {
              Console.WriteLine("Error: " + item.Path + " is invalid or exceeds the maximum path length");
              continue;
            }
            catch (Exception e) when (e is UnauthorizedAccessException)
            {
              Console.WriteLine("Error: Permissions error, cannot delete " + item.Name);
              continue;
            }
            catch (Exception e) when (e is DirectoryNotFoundException)
            {
              Console.WriteLine("Error: " + item.Path + " was not found");
              continue;
            }
            catch (Exception e)
            {
              Console.WriteLine("Error: " + e);
              continue;
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
              Console.WriteLine("Error: path is null or empty, or includes invalid characters");
              continue;
            }
            catch (Exception e) when (e is IOException)
            {
              Console.WriteLine("Error: " + item.Name + " cannot be deleted, is it open?");
              continue;
            }
            catch (Exception e) when (e is NotSupportedException or PathTooLongException)
            {
              Console.WriteLine("Error: " + item.Path + " is invalid or exceeds the maximum path length");
              continue;
            }
            catch (Exception e) when (e is UnauthorizedAccessException)
            {
              Console.WriteLine("Error: Permissions error, cannot delete " + item.Name);
              continue;
            }
            catch (Exception e)
            {
              Console.WriteLine("Error: " + e);
              continue;
            }
          }
        }
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

  private static void PurgeFromRecycleBinSTA(string file)
  {
    Shell shell = new Shell();
    Folder recycleBinFolder = shell.NameSpace(10);
    FolderItems recycleBinItems = recycleBinFolder.Items();
    IEnumerable<FolderItem> collection = recycleBinItems.Cast<FolderItem>();
    IEnumerable<FolderItem> query = from item in collection
      where item.Name.Contains(file)
      select item;

    Console.WriteLine("Purging...");
        foreach (FolderItem item in query)
        {
          if (item.IsFileSystem == false)
          {
            Console.WriteLine("File isFileSystem false, what do?"); // TODO
            continue;
          }
          else if (item.IsFolder == true)
          {
            try
            {
              Directory.Delete(item.Path, true);
            }
            catch (Exception e) when (e is ArgumentNullException or ArgumentException)
            {
              Console.WriteLine("Error: path is null or empty, or includes invalid characters");
              continue;
            }
            catch (Exception e) when (e is IOException)
            {
              Console.WriteLine("Error: " + item.Name + " cannot be deleted, is it in use?");
              continue;
            }
            catch (Exception e) when (e is PathTooLongException)
            {
              Console.WriteLine("Error: " + item.Path + " is invalid or exceeds the maximum path length");
              continue;
            }
            catch (Exception e) when (e is UnauthorizedAccessException)
            {
              Console.WriteLine("Error: Permissions error, cannot delete " + item.Name);
              continue;
            }
            catch (Exception e) when (e is DirectoryNotFoundException)
            {
              Console.WriteLine("Error: " + item.Path + " was not found");
              continue;
            }
            catch (Exception e)
            {
              Console.WriteLine("Error: " + e);
              continue;
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
              Console.WriteLine("Error: path is null or empty, or includes invalid characters");
              continue;
            }
            catch (Exception e) when (e is IOException)
            {
              Console.WriteLine("Error: " + item.Name + " cannot be deleted, is it open?");
              continue;
            }
            catch (Exception e) when (e is NotSupportedException or PathTooLongException)
            {
              Console.WriteLine("Error: " + item.Path + " is invalid or exceeds the maximum path length");
              continue;
            }
            catch (Exception e) when (e is UnauthorizedAccessException)
            {
              Console.WriteLine("Error: Permissions error, cannot delete " + item.Name);
              continue;
            }
            catch (Exception e)
            {
              Console.WriteLine("Error: " + e);
              continue;
            }
          }
        }
  }


}