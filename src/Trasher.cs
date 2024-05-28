using System.CommandLine;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;


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
      Argument<string> testArg = new Argument<string>(name: "test", description: "testArg");
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

      testCommand.AddArgument(testArg);
      testCommand.SetHandler(TestHandler, testArg);

      Console.OutputEncoding = System.Text.Encoding.UTF8;

      return await rootCommand.InvokeAsync(args);
    }

    static void DeleteHandler(FileSystemInfo file)
    {
      if (file.Exists)
      {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { RecycleBin.SendToRecycleBin(file); }
      }
    }

    static void RestoreHandler(string file)
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { RecycleBin.RestoreFromRecycleBin(file); }
    }

    static void ListHandler()
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        List<FileDetails> itemsList = RecycleBin.GetRecycleBinItems();
        if (itemsList.Count > 0)
        {
          HelperFunctions.WriteConsoleTable(itemsList);
        }
        else
        {
          Console.WriteLine("Trash is empty.");
        }
      }
    }

    static void EmptyHandler()
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { RecycleBin.EmptyRecycleBinContents(); }
    }

    static void PurgeHandler(string file)
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { RecycleBin.PurgeFromRecycleBin(file); }
    }

    static void TestHandler(string inputPath)
    {
      string processedPath = HelperFunctions.ProcessInputPathString(inputPath);

      Matcher matcher;
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
      }
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
        matcher = new Matcher(StringComparison.Ordinal);
      }

      Console.WriteLine(processedPath);

      if (processedPath.Contains('*'))
      {

      }




    }

  }
}


