using System.CommandLine;
using System.Net.Http.Headers;
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
      else
      {
        Console.WriteLine("Unsupported platform. Only Windows and Linux are supported");
        return;
      }

      // Restrict usage of arbitrary directory depth token (**)
      Regex unsupportedDoubleStar = new Regex(@"([^/]\*{2})|(\*{2}[^/])");

      if (unsupportedDoubleStar.IsMatch(processedPath))
      {
        Console.WriteLine("Invalid use of arbitrary directory depth token ('**')");
        return;
      }

      if (processedPath.Split("/**/").Length > 2)
      {
        Console.WriteLine("Only one instance of the arbitrary directory depth token ('**') is allowed in a path");
        return;
      }

      if (processedPath.Contains("/**/"))
      {
        string parentPathString = processedPath.Split("/**/")[0];
        matcher.AddInclude("/**/" + processedPath.Split("/**/")[1]);
        PatternMatchingResult result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(parentPathString)));
        if (result.HasMatches)
        {
          foreach (FilePatternMatch match in result.Files)
          {
            Console.WriteLine(parentPathString + Path.AltDirectorySeparatorChar + match.Path);
          }
          return;
        }
      }

      Regex containsSingleStar = new Regex(@"[^/\*]*\*[^/\*]*");

      if (containsSingleStar.IsMatch(processedPath))
      {
        int starIndex = processedPath.IndexOf('*');
        string parentPathString = processedPath.Substring(0, starIndex).TrimEnd('/');
        string pattern = processedPath.Substring(starIndex).TrimStart('/');
        matcher.AddInclude(pattern);
        PatternMatchingResult result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(parentPathString)));
        if (result.HasMatches)
        {
          foreach (FilePatternMatch match in result.Files)
          {
            Console.WriteLine(parentPathString + Path.AltDirectorySeparatorChar + match.Path);
          }
        }
      }








    }

  }
}


