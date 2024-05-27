using System;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using Shell32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

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


  public static void SendToTrash(FileInfo file)
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

  public static void SendToTrash(DirectoryInfo directory)
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

  static void RestoreFromTrash(FileInfo file)
  {

  }

  public static Tuple<long,long> GetTrashContentInfo()
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

      for (int i = 0; i < recycleBinFolder.Items().Count; i++)
      {
        FolderItem folderItem = recycleBinFolder.Items().Item(i);
        FileDetails fileDetails = new FileDetails()
        {
          Name = recycleBinFolder.GetDetailsOf(folderItem, 0),
          Size = recycleBinFolder.GetDetailsOf(folderItem, 3),
          OriginalPath = recycleBinFolder.GetDetailsOf(folderItem, 1),
          TimeDeleted = (recycleBinFolder.GetDetailsOf(folderItem, 2)).Replace("?", "").TrimStart().TrimEnd()
        };
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
      FileDetails fileDetails = new FileDetails()
      {
        Name = recycleBinFolder.GetDetailsOf(folderItem, 0),
        Size = recycleBinFolder.GetDetailsOf(folderItem, 3),
        OriginalPath = recycleBinFolder.GetDetailsOf(folderItem, 1),
        TimeDeleted = (recycleBinFolder.GetDetailsOf(folderItem, 2)).Replace("?", "")
      };
      recycleBinItems.Add(fileDetails);
    }

    return recycleBinItems;
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

}