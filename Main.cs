using System.Diagnostics;

namespace StandardXMLparser
{
  public static class CustomXmlForwardParser
  {
    public static async Task Main()
    {
      string filePath = "../../../standard.xml";
      string outputPath = "../../../complete_output.txt";
      string searchValue = "fence angel";
      string searchElement = "location";
            
      string xmlString = await File.ReadAllTextAsync(filePath);
                
      var parser = new StandardXmlParser();
      parser.SetOutputPath(outputPath);
                
      // Example usage of Parser methods
      parser.SetSearchValue(searchValue);
      parser.SetSearchAttribute("id", "item12");
      parser.SetSearchElement(searchElement);

      var stopwatch = Stopwatch.StartNew();

      parser.Parse(xmlString, true);

      stopwatch.Stop();
      Console.WriteLine($"Parsing took {stopwatch.ElapsedMilliseconds} ms.");
    }
  }
}