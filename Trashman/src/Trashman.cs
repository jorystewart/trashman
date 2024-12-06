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
      Command listCommand = new Command(name: "list", description: "List/search files currently in trash");
      Command emptyCommand = new Command(name: "empty", description: "Permanently delete all files in trash");
      Command purgeCommand = new Command(name: "purge", description: "Permanently delete a file from trash");

      Argument<List<string>> fileArg = new Argument<List<string>>(name: "file", description: "File(s) to delete");
      Argument<string>? searchArg = new Argument<string>(name: "search", description: "String to search for", getDefaultValue: () => string.Empty);
      Argument<List<string>> restoreArg = new Argument<List<string>>(name: "file", description: "File(s) to restore");
      Argument<List<string>> purgeArg = new Argument<List<string>>(name: "file", description: "File(s) to purge");

      rootCommand.AddCommand(deleteCommand);
      rootCommand.AddCommand(restoreCommand);
      rootCommand.AddCommand(listCommand);
      rootCommand.AddCommand(emptyCommand);
      rootCommand.AddCommand(purgeCommand);

      deleteCommand.AddArgument(fileArg);
      listCommand.AddArgument(searchArg);
      restoreCommand.AddArgument(restoreArg);
      purgeCommand.AddArgument(purgeArg);

      deleteCommand.SetHandler((filesToDelete) =>
      {
        foreach (string file in filesToDelete)
        {
          DeleteHandler(file);
        }
      }, fileArg);
      deleteCommand.AddAlias("d");
      deleteCommand.AddAlias("D");
      restoreCommand.SetHandler(RestoreHandler, restoreArg);
      restoreCommand.AddAlias("r");
      restoreCommand.AddAlias("R");
      listCommand.SetHandler(ListHandler, searchArg);
      listCommand.AddAlias("l");
      listCommand.AddAlias("L");
      emptyCommand.SetHandler(EmptyHandler);
      emptyCommand.AddAlias("e");
      emptyCommand.AddAlias("E");
      purgeCommand.SetHandler(PurgeHandler, purgeArg);
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

    static void RestoreHandler(List<string> file)
    {
      if (file.Count < 1)
      {
        return;
      }
      #if WINDOWS
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { RecycleBin.RestoreFromRecycleBin(file); }
      #endif
      #if LINUX
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { Trash.RestoreFromTrash(file);}
      #endif
    }

    static void ListHandler(string? search)
    {
      #if WINDOWS
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        List<FileDetails> itemsList = RecycleBin.GetRecycleBinItems();
        if (itemsList.Count <= 0)
        {
          Console.WriteLine("Trash is empty");
          return;
        }
        if (search == String.Empty)
        {
          HelperFunctions.WriteConsoleTable(itemsList);
        }
        else
        {
          IEnumerable<FileDetails> filteredList = from item in itemsList
                                                  where item.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase)
                                                  select item;
          if (filteredList.Any())
          {
            HelperFunctions.WriteConsoleTable(filteredList.ToList());
          }
        }
      }
      #endif
      #if LINUX
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
        List<FileDetails> itemsList = Trash.GetTrashContents();
        if (itemsList.Count <= 0)
        {
          Console.WriteLine("Trash is empty");
          return;
        }
        if (search == String.Empty)
        {
          HelperFunctions.WriteConsoleTable(itemsList);
        }
        else
        {
          IEnumerable<FileDetails> filteredList = from item in itemsList
                                                  where item.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase)
                                                  select item;
          if (filteredList.Any())
          {
            HelperFunctions.WriteConsoleTable(filteredList.ToList());
          }
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

    static void PurgeHandler(List<string> file)
    {
      if (file.Count < 1)
      {
        return;
      }
      #if WINDOWS
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { RecycleBin.PurgeFromRecycleBin(file); }
      #endif
      #if LINUX
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { Trash.PurgeFromTrash(file);}
      #endif
    }

  }
}


