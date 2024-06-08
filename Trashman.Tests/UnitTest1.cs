using Xunit;


namespace Trashman.Tests;

public class Setup : IDisposable
{

  private readonly string _baseTestDir = @"D:\source\repos\jorystewart\trashman\tests";
  private readonly List<string> filesToCreate = [
    @"test.txt",
    @"a.b"
  ];
  private readonly List<string> directoriesToCreate = new List<string>();


  protected Setup()
  {
    if (Directory.Exists(_baseTestDir))
    {
      // Fail test
    }

    Directory.CreateDirectory(_baseTestDir);

    foreach (string item in directoriesToCreate)
    {
      Directory.CreateDirectory(_baseTestDir + item);
    }

    foreach (string item in filesToCreate)
    {
      File.Create(_baseTestDir + item);
    }




  }

  public void Dispose()
  {
    Directory.Delete(_baseTestDir, true);
  }




}

public class RecycleBinTests
{
  [Fact]
  public void Test1()
  {
  }
}