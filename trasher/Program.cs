using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Trasher
{
  public static class Trasher
  {
    static async Task<int> Main(string[] args)
    {
      Option addOption = new Option<FileInfo?>(name: "--file", description: "File to move to trash");
      addOption.AddAlias("-f");
      Option restoreOption = new Option<FileInfo?>(name: "--restore", description: "File to restore from trash");
      restoreOption.AddAlias("-r");
      Option listOption = new Option<bool>(name: "--list-contents", description: "List the contents of trash");
      listOption.AddAlias("-l");
      Option emptyOption = new Option<bool>(name: "--empty", description: "Empty trash");
      emptyOption.AddAlias("-e");
      Option deleteOption = new Option<FileInfo?>(name: "--delete", description: "File to permanently delete");
      deleteOption.AddAlias("-d");
      Argument<FileInfo> fileArg = new Argument<FileInfo>(name: "file", description: "File to move to trash");
      RootCommand rootCommand = new RootCommand("CLI trash/recycle bin management tool");
      rootCommand.AddOption(addOption);
      rootCommand.AddOption(restoreOption);
      rootCommand.AddOption(listOption);
      rootCommand.AddOption(emptyOption);
      rootCommand.AddOption(deleteOption);
      rootCommand.AddArgument(fileArg);

      rootCommand.SetHandler();

      return await rootCommand.InvokeAsync(args);
    }

    static void ReadFile(FileInfo file)
    {
      File.ReadLines(file.FullName).ToList().ForEach(line => Console.WriteLine(line));
    }
  }
}


