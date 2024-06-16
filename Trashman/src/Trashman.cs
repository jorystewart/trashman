using System.CommandLine;
using System.Runtime.InteropServices;

namespace Trashman
{
  public static class Trashman
  {
    static async Task<int> Main(string[] args)
    {
      RootCommand rootCommand = new RootCommand("CLI trash/recycle bin management tool");
      Command deleteCommand = new Command(name: "delete", description: "Move a file to trash");
      Command restoreCommand = new Command(name: "restore", description: "Restore a file from trash");
      Command listCommand = new Command(name: "list", description: "List files currently in trash");
      Command emptyCommand = new Command(name: "empty", description: "Permanently delete all files in trash");
      Command purgeCommand = new Command(name: "purge", description: "Permanently delete a file from trash");

      Argument<string> fileArg = new Argument<string>(name: "file", description: "Target file");
      Option<string> deleteOption = new Option<string>(aliases: ["--file", "-d"], description: "Target file");
      deleteOption.AllowMultipleArgumentsPerToken = true;
      deleteOption.IsRequired = true;
      Argument<string> searchArg = new Argument<string>(name: "file", description: "File name to search for");
      Option<string> searchOption = new Option<string>(name: "file", description: "File name to search for");

      rootCommand.AddCommand(deleteCommand);
      rootCommand.AddOption(deleteOption);
      rootCommand.AddCommand(restoreCommand);
      rootCommand.AddCommand(listCommand);
      rootCommand.AddCommand(emptyCommand);
      rootCommand.AddCommand(purgeCommand);

      //deleteCommand.AddArgument(fileArg);
      deleteCommand.AddOption(deleteOption);
      restoreCommand.AddArgument(searchArg);
      purgeCommand.AddArgument(searchArg);

      //deleteCommand.SetHandler(DeleteHandler, fileArg);
      deleteCommand.AddAlias("d");
      deleteCommand.AddAlias("D");
      restoreCommand.SetHandler(RestoreHandler, searchArg);
      restoreCommand.AddAlias("r");
      restoreCommand.AddAlias("R");
      listCommand.SetHandler(ListHandler);
      listCommand.AddAlias("l");
      listCommand.AddAlias("L");
      emptyCommand.SetHandler(EmptyHandler);
      emptyCommand.AddAlias("e");
      emptyCommand.AddAlias("E");
      purgeCommand.SetHandler(PurgeHandler, searchArg);
      purgeCommand.AddAlias("p");
      purgeCommand.AddAlias("P");

      Console.OutputEncoding = System.Text.Encoding.UTF8;

      return await rootCommand.InvokeAsync(args);
    }

    static void DeleteHandler(string file)
    {
      string processedPath = HelperFunctions.ProcessInputPathString(file);
      List<string> searchResults = HelperFunctions.PerformGlobSearch(processedPath);

      foreach (string result in searchResults)
      {
        if (File.Exists(result))
        {
          #if WINDOWS
          if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { RecycleBin.SendToRecycleBin(new FileInfo(result)); }
          #endif
          #if LINUX
          if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { Trash.SendToTrash(new FileInfo(result)); }
          #endif
        }
        else if (Directory.Exists(result))
        {
          #if WINDOWS
          if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { RecycleBin.SendToRecycleBin(new DirectoryInfo(result)); }
          #endif
          #if LINUX
          if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { Trash.SendToTrash(new DirectoryInfo(result)); }
          #endif
        }
      }
    }

    static void RestoreHandler(string file)
    {
      if (file.Contains("**") || file.Contains('/')) { return; }

      #if WINDOWS
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { RecycleBin.RestoreFromRecycleBin(file); }
      #endif
      #if LINUX
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { Trash.RestoreFromTrash(file);}
      #endif
    }

    static void ListHandler()
    {
      #if WINDOWS
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
      #endif
      #if LINUX
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
        List<FileDetails> itemsList = Trash.GetTrashContents();
        if (itemsList.Count > 0)
        {
          HelperFunctions.WriteConsoleTable(itemsList);
        }
        else
        {
          Console.WriteLine("Trash is empty.");
        }
      }
      #endif
    }

    static void EmptyHandler()
    {
      #if WINDOWS
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { RecycleBin.EmptyRecycleBinContents(); }
      #endif
      #if LINUX
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { Trash.EmptyTrashContents();}
      #endif
    }

    static void PurgeHandler(string file)
    {
      if (file.Contains("**") || file.Contains('/')) { return; }

      #if WINDOWS
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { RecycleBin.PurgeFromRecycleBin(file); }
      #endif
      #if LINUX
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { Trash.purgeFromTrash(file);}
      #endif
    }

  }
}


