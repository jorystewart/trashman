using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Shell32;


namespace Trasher
{
  public static class Trasher
  {
    static async Task<int> Main(string[] args)
    {
      RootCommand rootCommand = new RootCommand("CLI trash/recycle bin management tool");
      Command deleteCommand = new Command(name: "delete", description: "Move a file to trash");
      Command restoreCommand = new Command(name: "restore", description: "Restore a file from trash");
      Command listCommand = new Command(name: "list", description: "List files currently in trash");
      Command emptyCommand = new Command(name: "empty", description: "Permanently delete all files in trash");
      Command purgeCommand = new Command(name: "purge", description: "Permanently delete a file from trash");
      Command testCommand = new Command(name: "test", description: "For testing purposes");

      Argument<FileSystemInfo> fileArg = new Argument<FileSystemInfo>(name: "file", description: "Target file");
      Argument<string> searchArg = new Argument<string>(name: "file", description: "File name to search for");


      rootCommand.AddCommand(deleteCommand);
      rootCommand.AddCommand(restoreCommand);
      rootCommand.AddCommand(listCommand);
      rootCommand.AddCommand(emptyCommand);
      rootCommand.AddCommand(purgeCommand);
      rootCommand.AddCommand(testCommand);

      deleteCommand.AddArgument(fileArg);
      restoreCommand.AddArgument(searchArg);
      purgeCommand.AddArgument(searchArg);

      deleteCommand.SetHandler(DeleteHandler, fileArg);
      restoreCommand.SetHandler(RestoreHandler, searchArg);
      listCommand.SetHandler(ListHandler);
      emptyCommand.SetHandler(EmptyHandler);
      purgeCommand.SetHandler(PurgeHandler, searchArg);

      testCommand.AddArgument(searchArg);
      testCommand.SetHandler(TestHandler, searchArg);

      Console.OutputEncoding = System.Text.Encoding.UTF8;


      return await rootCommand.InvokeAsync(args);
    }

    static void DeleteHandler(FileSystemInfo file)
    {
      RecycleBin.SendToTrashWrapper(file);
    }

    static void RestoreHandler(string file)
    {
      RecycleBin.RestoreFromTrash(file);
    }

    static void ListHandler()
    {
      List<FileDetails> itemsList = RecycleBin.GetTrashItems();
      if (itemsList.Count > 0)
      {
        HelperFunctions.WriteConsoleTable(itemsList);
      }
      else
      {
        Console.WriteLine("Trash is empty.");
      }
    }

    static void EmptyHandler()
    {
      RecycleBin.EmptyTrashContents();
    }

    static void PurgeHandler(string file)
    {
      RecycleBin.PurgeFromTrash(file);
    }

    static void TestHandler(string file)
    {
      RecycleBin.RestoreFromTrash(file);
    }



  }
}


