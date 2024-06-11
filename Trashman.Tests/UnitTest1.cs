using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Trashman.Tests;

public class RecycleBinTestsSetup : IDisposable
{

  private readonly string _baseTestDir = @"D:\source\repos\jorystewart\trashman\tests\";
  private readonly List<string> _filesToCreate = [
    @"test.txt",
    @"a.b",
    @"text.txt",
    @"5444aaaafimif9.json",
    @"RecursiveTest\aaa.json",
    @"RecursiveTest\Recursion1\bbb.json",
    @"RecursiveTest\aaa.txt",
    @"RecursiveTest\Recursion2\aaa.txt"
  ];
  private readonly List<string> _directoriesToCreate = [
    @"TestDir",
    @"RecursiveTest",
    @"RecursiveTest\Recursion1",
    @"RecursiveTest\Recursion2"
  ];


  protected RecycleBinTestsSetup()
  {
    if (Directory.Exists(_baseTestDir))
    {
      // Fail test
    }

    Directory.CreateDirectory(_baseTestDir);

    foreach (string item in _directoriesToCreate)
    {
      Directory.CreateDirectory(_baseTestDir + item);
    }

    foreach (string item in _filesToCreate)
    {
      File.Create(_baseTestDir + item);
    }


  }

  public void Dispose()
  {
    Directory.Delete(_baseTestDir, true);
  }

  [Fact]
  public void FileDeletionTest()
  {
  }

  [Fact]
  public void FileRestoreTest()
  {

  }


}
