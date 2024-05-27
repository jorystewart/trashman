using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;


namespace Trasher
{
  public static class Trasher
  {
    [STAThread]
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


      rootCommand.AddCommand(deleteCommand);
      rootCommand.AddCommand(restoreCommand);
      rootCommand.AddCommand(listCommand);
      rootCommand.AddCommand(emptyCommand);
      rootCommand.AddCommand(purgeCommand);
      rootCommand.AddCommand(testCommand);

      deleteCommand.AddArgument(fileArg);
      restoreCommand.AddArgument(fileArg);
      purgeCommand.AddArgument(fileArg);

      deleteCommand.SetHandler(DeleteHandler, fileArg);

      restoreCommand.SetHandler(file =>
      {
        RestoreHandler();
      }, fileArg);

      listCommand.SetHandler(ListHandler);
      emptyCommand.SetHandler(EmptyHandler);
      purgeCommand.SetHandler(PurgeHandler);

      testCommand.SetHandler(TestHandler);

      Console.OutputEncoding = System.Text.Encoding.UTF8;


      return await rootCommand.InvokeAsync(args);
    }

    static void DeleteHandler(FileSystemInfo file)
    {
      if ((file.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
      {
        RecycleBin.SendToTrash((DirectoryInfo)file);
      }
      else
      {
        RecycleBin.SendToTrash((FileInfo)file);
      }
    }

    static void RestoreHandler()
    {
      Console.WriteLine("Not implemented");

    }

    static void ListHandler()
    {
      List<FileDetails> itemsList = RecycleBin.GetRecycleBinItems();
      HelperFunctions.WriteConsoleTable(itemsList);
    }

    static void EmptyHandler()
    {
      RecycleBin.EmptyTrashContents();
    }

    static void PurgeHandler()
    {
      Console.WriteLine("Not implemented");
    }

    static void TestHandler()
    {
      List<FileDetails> itemsList = RecycleBin.GetRecycleBinItems();
      HelperFunctions.WriteConsoleTable(itemsList);
    }



  }
}


