using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualBasic;

namespace Trasher;

public class Trash
{
  private static string _trashLocation = (Environment.GetEnvironmentVariable("XDG_DATA_HOME") == (String.Empty) || Environment.GetEnvironmentVariable("XDG_DATA_HOME") == null) ? Environment.GetEnvironmentVariable("XFG_DATA_HOME") + "/Trash" : Environment.GetEnvironmentVariable("HOME") + "/.local/share/Trash";

  public static void SendToTrash(FileSystemInfo file)
  {
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

          fileStream = File.Create(_trashLocation + "/info/" + trashFileName + ".trashinfo");
          writer = new StreamWriter(fileStream);
          writer.AutoFlush = true;
          writer.Write("[Trash Info");
          writer.Write("Path=" + fileInfo.FullName);
          writer.Write("DeletionDate=" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));

          writer.Close();
          fileStream.Close();
          File.Move(fileInfo.FullName, _trashLocation + "/files/");
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

        fileStream = File.Create(_trashLocation + "/info/" + trashFileName + ".trashinfo");
        writer = new StreamWriter(fileStream);
        writer.Write("[Trash Info");
        writer.Write("Path=" + directoryInfo.FullName);
        writer.Write("DeletionDate=" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
        writer.Close();
        fileStream.Close();
        Directory.Move(directoryInfo.FullName, _trashLocation + "/files");
        break;
    }
  }

  public static void RestoreFromTrash(string file)
  {
    file = HelperFunctions.ProcessInputPathString(file);

    // TODO
  }

  public static List<FileDetails> GetTrashContents()
  {
    DirectoryInfo trashInfoDir = new DirectoryInfo(_trashLocation + "/info");
    List<FileDetails> trashContents = new List<FileDetails>();
    FileInfo[] trashInfoFiles = trashInfoDir.GetFiles();
    foreach (FileInfo infoFile in trashInfoFiles)
    {
      FileStream stream = infoFile.Open(FileMode.Open);
      StreamReader reader = new StreamReader(stream);
      FileDetails fileDetails = new FileDetails();
      string? readString;
      while (!reader.EndOfStream)
      {
        readString = reader.ReadLine();
        if (readString[0] == '[')
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

      string[] originalFile = Directory.GetFiles(_trashLocation + "/files", infoFile.Name);
      if (File.Exists(_trashLocation + "/files" + originalFile[0]))
      {
        fileDetails.Size = HelperFunctions.ConvertBytes((new FileInfo(_trashLocation + "/files/" + originalFile[0])).Length);
      }
      trashContents.Add(fileDetails);

    }

    return trashContents;
  }

  public static void EmptyTrashContents()
  {
    DirectoryInfo trashFilesDir = new DirectoryInfo(_trashLocation + "/files");
    DirectoryInfo trashInfoDir = new DirectoryInfo(_trashLocation + "/info");
    Console.WriteLine(trashFilesDir.GetFiles().Length + " items in trash.");
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
              Directory.Delete(directoryInfo.FullName);
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
    file = HelperFunctions.ProcessInputPathString(file);

    // TODO
  }






}



