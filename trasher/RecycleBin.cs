using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using Shell32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using System.Linq;

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

  [Flags]
  enum FileOperationFlags : uint
  {
    FO_MOVE = 0x0001,
    FO_COPY = 0x0002,
    FO_DELETE = 0x0003,
    FO_RENAME = 0x0004
  }

  #endregion

  #region Shell32.dll Structs

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
  struct SHFILEOPSTRUCT
  {
    public IntPtr hwnd;
    [MarshalAs(UnmanagedType.U4)] public int wFunc;
    public string pFrom;
    public string pTo;
    public short fFlags;
    [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
    public IntPtr hNameMappings;
    public string lpszProgressTitle;
  }

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
  static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

  [DllImport("shell32.dll")]
  static extern int SHQueryRecycleBinW(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

  [DllImport("shell32.dll")]
  static extern int SHEmptyRecycleBinW(IntPtr hwnd, string pszRootPath, uint dwFlags);

  #endregion



  public static void SendToTrashWrapper(FileSystemInfo file)
  {
    if ((file.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
    {
      SendToTrash((DirectoryInfo)file);
    }
    else
    {
      SendToTrash((FileInfo)file);
    }
  }

  private static void SendToTrash(FileInfo file)
  {
    try
    {
      if (file.Exists)
      {
        FileSystem.DeleteFile(file.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
      }
    }
    catch (FileNotFoundException e)
    {
      Console.WriteLine("File not found:" + file.FullName);
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      throw;
    }
  }

  private static void SendToTrash(DirectoryInfo directory)
  {
    try
    {
      if (directory.Exists)
      {
        FileSystem.DeleteDirectory(directory.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      throw;
    }
  }

  public static List<FolderItem> SearchTrash(string file)
  {
    List<FolderItem> matches = new List<FolderItem>();
    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
    {
      Shell shell = new Shell();
      Folder trashFolder = shell.NameSpace(10);
      FolderItems trashItems = trashFolder.Items();
      IEnumerable<FolderItem> collection = trashItems.Cast<FolderItem>();
      IEnumerable<FolderItem> query = from item in collection
        where item.Name.Contains(file)
        select item;
      foreach (FolderItem match in query)
      {
        matches.Add(match);
      }
      return matches;
    }
    else
    {
      Thread staThread = new Thread(
        () => { matches = SearchTrashSTA(file); });
      staThread.SetApartmentState(ApartmentState.STA);
      staThread.Start();
      staThread.Join();
      return matches;
    }
  }
  private static List<FolderItem> SearchTrashSTA(string file)
  {
    Shell shell = new Shell();
    Folder trashFolder = shell.NameSpace(10);
    FolderItems trashItems = trashFolder.Items();
    List<FolderItem> matches = new List<FolderItem>();
    IEnumerable<FolderItem> collection = trashItems.Cast<FolderItem>();
    IEnumerable<FolderItem> query = from item in collection
      where item.Name.Contains(file)
      select item;
    foreach (FolderItem match in query)
    {
      matches.Add(match);
    }
    return matches;
  }

  public static void RestoreFromTrash(string file)
  {
    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
    {
      Shell shell = new Shell();
      Folder trashFolder = shell.NameSpace(10);
      FolderItems trashItems = trashFolder.Items();
      IEnumerable<FolderItem> collection = trashItems.Cast<FolderItem>();
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
        () => { RestoreFromTrashSTA(file); });
      staThread.SetApartmentState(ApartmentState.STA);
      staThread.Start();
      staThread.Join();
    }
  }

  private static void RestoreFromTrashSTA(string file)
  {
    Shell shell = new Shell();
    Folder trashFolder = shell.NameSpace(10);
    FolderItems trashItems = trashFolder.Items();
    IEnumerable<FolderItem> collection = trashItems.Cast<FolderItem>();
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



  public static Tuple<long,long> GetTrashContentInfo()
  {
    SHQUERYRBINFO trashQueryInfo = new SHQUERYRBINFO();
    trashQueryInfo.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));
    int queryHResult = SHQueryRecycleBinW(String.Empty, ref trashQueryInfo);
    if (queryHResult != 0)
    {
      Console.WriteLine("Error querying Recycle Bin contents. HRESULT: " + queryHResult);
      throw new Exception("Error querying Recycle Bin contents. HRESULT: " + queryHResult);
    }
    else
    {
      return new Tuple<long, long>(trashQueryInfo.i64NumItems, trashQueryInfo.i64Size);
    }
  }


  public static List<FileDetails> GetTrashItems()
  {
    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
    {
      List<FileDetails> trashItems = new List<FileDetails>();
      Shell shell = new Shell();
      Folder trashFolder = shell.NameSpace(10);

      if (trashFolder.Items().Count < 1)
      {
        return trashItems;
      }
      for (int i = 0; i < trashFolder.Items().Count; i++)
      {
        FolderItem folderItem = trashFolder.Items().Item(i);
        FileDetails fileDetails = new FileDetails()
        {
          Name = trashFolder.GetDetailsOf(folderItem, 0),
          Size = trashFolder.GetDetailsOf(folderItem, 3),
          OriginalPath = trashFolder.GetDetailsOf(folderItem, 1),
          TimeDeleted = (trashFolder.GetDetailsOf(folderItem, 2)).Replace("?", "").TrimStart().TrimEnd()
        };
        trashItems.Add(fileDetails);
      }
      return trashItems;
    }
    else
    {
      List<FileDetails> trashItems = new List<FileDetails>();
      Thread staThread = new Thread(
        () => { trashItems = GetTrashItemsSTA(); });
      staThread.SetApartmentState(ApartmentState.STA);
      staThread.Start();
      staThread.Join();
      return trashItems;
    }
  }


  private static List<FileDetails> GetTrashItemsSTA()
  {
    List<FileDetails> trashItems = new List<FileDetails>();
    Shell shell = new Shell();
    Folder trashFolder = shell.NameSpace(10);

    for (int i = 0; i < trashFolder.Items().Count; i++)
    {
      FolderItem folderItem = trashFolder.Items().Item(i);
      FileDetails fileDetails = new FileDetails()
      {
        Name = trashFolder.GetDetailsOf(folderItem, 0),
        Size = trashFolder.GetDetailsOf(folderItem, 3),
        OriginalPath = trashFolder.GetDetailsOf(folderItem, 1),
        TimeDeleted = (trashFolder.GetDetailsOf(folderItem, 2)).Replace("?", "")
      };
      trashItems.Add(fileDetails);
    }

    return trashItems;
  }


  public static void EmptyTrashContents()
  {
    uint flags = (uint)(RecycleBinFlags.SHERB_NOCONFIRMATION | RecycleBinFlags.SHERB_NOPROGRESSUI |
                  RecycleBinFlags.SHERB_NOSOUND);
    Tuple<long,long> trashContentInfo = GetTrashContentInfo();
    if (trashContentInfo.Item1 != 0)
    {
      Console.WriteLine(trashContentInfo.Item1 + " items found in Recycle Bin (" + HelperFunctions.ConvertBytes(trashContentInfo.Item2) + ")");
      Console.WriteLine("Confirm deletion? Y/(N)");
      ConsoleKeyInfo confirmKey = Console.ReadKey(true);
      if (confirmKey.Key == ConsoleKey.Y)
      {
        int hrResult = SHEmptyRecycleBinW(IntPtr.Zero, String.Empty, flags);
        if (hrResult == 0)
        {
          Console.WriteLine("Recycle Bin emptied.");
        }
        else
        {
          Console.WriteLine("Error"); // TODO
        }
      }
      else
      {
        Console.WriteLine("Recycle Bin not emptied.");
      }
    }
  }

  public static void PurgeFromTrash(string file)
  {
    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
    {
      Shell shell = new Shell();
      Folder trashFolder = shell.NameSpace(10);
      FolderItems trashItems = trashFolder.Items();
      IEnumerable<FolderItem> collection = trashItems.Cast<FolderItem>();
      IEnumerable<FolderItem> query = from item in collection
        where item.Name.Contains(file)
        select item;
      if (query.Count() == 1)
      {
        Console.WriteLine("Purging...");
        foreach (FolderItem item in query)
        {
          foreach (FolderItemVerb verb in item.Verbs())
          {
            if (verb.Name.Contains("Delete") || (verb.Name.Contains("&Delete")))
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
        () => { PurgeFromTrashSTA(file); });
      staThread.SetApartmentState(ApartmentState.STA);
      staThread.Start();
      staThread.Join();
    }
  }

  private static void PurgeFromTrashSTA(string file)
  {
    Shell shell = new Shell();
    Folder trashFolder = shell.NameSpace(10);
    FolderItems trashItems = trashFolder.Items();
    IEnumerable<FolderItem> collection = trashItems.Cast<FolderItem>();
    IEnumerable<FolderItem> query = from item in collection
      where item.Name.Contains(file)
      select item;
    if (query.Count() == 1)
    {
      Console.WriteLine("Purging...");
      foreach (FolderItem item in query)
      {
        foreach (FolderItemVerb verb in item.Verbs())
        {
          if (verb.Name.Contains("Delete") || (verb.Name.Contains("&Delete")))
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



}