using System.Text;

namespace StandardXMLparser
{
  public class StandardXmlParser
  {
        
    private StreamWriter? _streamWriter;
    private string? _searchValue; 
    private string? _searchElement; 
    private string? _searchAttribute; 
    private string? _searchAttributeValue; 
    private bool _trackPaths;
    private Stack<string>? _elementStack; 
    private StringBuilder? _currentContent; 
    private bool _insideTargetElement; 

    public void SetOutputPath(string outputPath)
    {
      _streamWriter?.Dispose();
      _streamWriter = new StreamWriter(outputPath, append: false);
    }
        
    public void SetSearchValue(string searchValue) => _searchValue = searchValue;
    public void SetSearchElement(string searchElement) => _searchElement = searchElement;
    public void SetSearchAttribute(string searchAttribute, string searchAttributeValue)
    {
      _searchAttribute = searchAttribute;
      _searchAttributeValue = searchAttributeValue;
    }
    public void Parse(string xmlString, bool trackPaths)
    {
      if (_streamWriter == null)
      {
        throw new InvalidOperationException("Output path has not been set. Call SetOutputPath first.");
      }

      _trackPaths = trackPaths;
      _elementStack = new Stack<string>();
      _currentContent = new StringBuilder();
      _insideTargetElement = false;

      var currentText = new StringBuilder();
      ParserState state = ParserState.Start;

      using (var reader = new StringReader(xmlString))
      {
        int currentChar;
        while ((currentChar = reader.Read()) != -1)
        {
          char ch = (char)currentChar;

          switch (state)
          {
            case ParserState.Start:
              if (ch == '<')
              {
                if (reader.Peek() == '!')
                {
                  state = ParserState.InComment;
                  reader.Read();
                  currentText.Clear();
                }
                else
                {
                  state = ParserState.InElement;
                  currentText.Clear();
                }
              }
              else
              {
                currentText.Append(ch);
                state = ParserState.InText;
              }
              break;

            case ParserState.InComment:
              currentText.Append(ch);
              if (currentText.Length >= 3 && currentText.ToString().EndsWith("-->"))
              {
                state = ParserState.Start;
                currentText.Clear();
              }
              break;

            case ParserState.InElement:
              if (ch == '/')
              {
                state = ParserState.EndElement;
              }
              else if (ch == '>')
              {
                HandleStartElement(currentText.ToString().Trim());
                currentText.Clear();
                state = ParserState.Start;
              }
              else
              {
                currentText.Append(ch);
              }
              break;

            case ParserState.EndElement:
              if (ch == '>')
              {
                HandleEndElement(currentText.ToString().Trim());
                currentText.Clear();
                state = ParserState.Start;
              }
              else
              {
                currentText.Append(ch);
              }
              break;

            case ParserState.InText:
              if (ch == '<')
              {
                HandleText(currentText.ToString().Trim());
                currentText.Clear();
                state = ParserState.InElement;
              }
              else
              {
                currentText.Append(ch);
              }
              break;
            }
        }

        //Make sure all content is written into the file before the parsing finishes
        if (_insideTargetElement && _currentContent.Length > 0)
        {
          FireElementParsedEvent($"{GetCurrentPath()} = \"{_currentContent.ToString().Trim()}\"");
        }
      }

      _streamWriter?.Dispose();
    }

    private void HandleStartElement(string elementName)
    {
      //Skip XML declaration
      if (elementName.StartsWith("?xml", StringComparison.OrdinalIgnoreCase))
      {
        return;
      }
            
      if (_trackPaths)
      {
        _elementStack?.Push(GetElementName(elementName));
      }
            
      //If we find the target element, start tracking its content
      if (!string.IsNullOrEmpty(_searchElement) && elementName.StartsWith(_searchElement, StringComparison.OrdinalIgnoreCase))
      {
        _insideTargetElement = true;
        _currentContent?.Clear();
      }

      //If we find the target attribute, write the path to the output
      if (!string.IsNullOrEmpty(_searchAttribute) && elementName.Contains($"{_searchAttribute}=\"{_searchAttributeValue}\""))
      {
        FireElementParsedEvent($"{GetCurrentPath()} contains attribute {_searchAttribute}=\"{_searchAttributeValue}\"");
      }
    }

    private void HandleEndElement(string elementString)
    {   
      //If we find the target element, write its content to the output
      if (_insideTargetElement && GetElementName(elementString).Equals(_searchElement, StringComparison.OrdinalIgnoreCase))
      {
        FireElementParsedEvent($"{GetCurrentPath()} = \"{_currentContent!.ToString().Trim()}\"");
        _insideTargetElement = false;
        _currentContent?.Clear();
      }
            
      //Pop the last element from the stack when we find its closing tag
      if (_trackPaths && _elementStack?.Count > 0 && _elementStack.Peek() == GetElementName(elementString))
      {
        _elementStack.Pop();
      }
    }

    private void HandleText(string textValue)
    {   
      //Escape special characters
      textValue = DecodeXmlEntities(textValue);
            
      //If the searched value is found in the text, write the path to the output together with the found content
      if (!string.IsNullOrEmpty(_searchValue) && textValue.Contains(_searchValue, StringComparison.OrdinalIgnoreCase))
      {
        FireElementParsedEvent($"{GetCurrentPath()} = \"{textValue}\"");
      }

      if (_insideTargetElement)
      {
        _currentContent?.Append(textValue);
      }
      //When the paths are not tracked, write all the text content to the output file
      if (!_trackPaths)
      {
        FireElementParsedEvent($"{textValue}");
      }
    }

        
    //Method to get the element name from the full element string (e.g. "element attr1="value"")
    private string GetElementName(string elementString)
    {
      int spaceIndex = elementString.IndexOf(' ');
      return (spaceIndex > 0) ? elementString.Substring(0, spaceIndex) : elementString;
    }
        
    private string GetCurrentPath()
    {
      return "/" + string.Join("/", _elementStack?.ToArray().Reverse() ?? Enumerable.Empty<string>());
    }

    private void FireElementParsedEvent(string elementInfo)
    {
      _streamWriter?.WriteLine($"{elementInfo}");
    }

    private enum ParserState
    {
      Start,
      InElement,
      InText,
      EndElement,
      InComment
    }
        
    private string DecodeXmlEntities(string text)
    {
      return text.Replace("&lt;", "<")
                 .Replace("&gt;", ">")
                 .Replace("&amp;", "&")
                 .Replace("&apos;", "'")
                 .Replace("&quot;", "\"");
    }
  }
}
