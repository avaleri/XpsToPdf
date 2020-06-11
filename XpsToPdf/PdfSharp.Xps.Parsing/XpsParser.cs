﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.IO;
using PdfSharp.Xps.XpsModel;

namespace PdfSharp.Xps.Parsing
{
  /// <summary>
  /// Simple XPS parser.
  /// The parser is not a syntax checker and therefore expects well-defined XPS XML to work properly.
  /// </summary>
  partial class XpsParser
  {
    XpsParser(XmlTextReader rdr)
    {
      reader = rdr;
    }

    /// <summary>
    /// 1st hack...
    /// </summary>
    public static XpsElement Parse(string xml)
    {
      using (StringReader sr = new StringReader(xml))
      {
        using (XmlTextReader rdr = new XmlTextReader(sr))
        {
          return Parse(rdr);
        }
      }
    }

    public static XpsElement Parse(XmlTextReader xmlReader)
    {
      XpsParser parser = new XpsParser(xmlReader);
      XpsElement element = parser.Parse();
      return element;
    }

    XpsElement Parse()
    {
      if (!reader.Read())
        return null;

      XpsElement element;

#if DEBUG_
      if (this.reader.NodeType == XmlNodeType.Comment)
        GetType();
#endif

      while (reader.NodeType == XmlNodeType.XmlDeclaration || reader.NodeType == XmlNodeType.Comment)
        MoveBeyondThisElement();

      if (reader.NodeType == XmlNodeType.Element)
      {
        element = ParseElement();
      }
#if true
#else
      else if (this.reader.NodeType == XmlNodeType.Comment || this.reader.NodeType == XmlNodeType.XmlDeclaration)
      {
        // ???
        FixedPage fpage = new FixedPage();
        Comment comment = new Comment();
        comment.Text = this.reader.Value;
        fpage.Content.Add(comment);
        element = fpage;
        //((Comment)element).Text = this.reader.Value;
      }
#endif
      else
      {
        throw new InvalidOperationException(PSXSR.ElementExpected);
      }
      return element;
    }

    XpsElement ParseElement()
    {
      if (reader.NodeType != XmlNodeType.Element)
        throw new InvalidOperationException(PSXSR.MustStandOnElement);

      XpsElement element = null;
      switch (reader.Name)
      {
        case "Canvas":
          element = ParseCanvas();
          break;

        case "FixedPage":
          element = ParseFixedPage();
          break;

        case "Glyphs":
          element = ParseGlyphs();
          break;

        case "MatrixTransform":
          element = ParseMatrixTransform();
          break;

        case "Path":
          element = ParsePath();
          break;

        case "FixedDocumentSequence":
          element = ParseFixedDocumentSequence();
          break;

        case "FixedDocument":
          element = ParseFixedDocument();
          break;

        case "DocumentReference":
          // TODO
          MoveBeyondThisElement();
          break;

        case "mc:AlternateContent":
          // TODO
          MoveBeyondThisElement();
          break;

        default:
          Debugger.Break();
          break;
      }
      return element;
    }

    /// <summary>
    /// Parses a boolean value element.
    /// </summary>
    bool ParseBool(string value)
    {
      return Boolean.Parse(value);
    }

    /// <summary>
    /// Parses a double value element.
    /// </summary>
    internal static double ParseDouble(string value)
    {
      return Double.Parse(value.Replace(" ", ""), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses an enum value element.
    /// </summary>
    T ParseEnum<T>(string value) where T : struct
    {
      return (T)Enum.Parse(typeof(T), value);
    }

    /// <summary>
    /// Parses a static resource element.
    /// </summary>
    T ParseStaticResource<T>(string value) where T : XpsElement
    {
      if (!value.StartsWith("{StaticResource"))
        throw new InvalidOperationException("Unexpected: " + value);

      string key = value.Substring("{StaticResource ".Length);
      key = key.Substring(0, key.IndexOf('}')).Trim();

      T res = FindStaticResource<T>(key, ResourceDictionaryStack.Current);
      if (res == null)
        throw new ArgumentException("StaticResource not found: " + value);
      return res;
    }

    /// <summary>
    /// Parses a static resource element if value is a static resource key, otherwise returns null.
    /// </summary>
    T TryParseStaticResource<T>(string value) where T : XpsElement
    {
      if (!value.StartsWith("{StaticResource"))
        return null;
      return ParseStaticResource<T>(value);
    }

    static T FindStaticResource<T>(string key, ResourceDictionary dict) where T : XpsElement
    {
      XpsElement elem = null;
      while (dict != null)
      {
        elem = dict[key];
        if (elem != null)
          break;
        dict = dict.ResourceParent;
      }
      T result = elem as T;
      if (elem != null && result == null)
        throw new InvalidCastException("Resource type mismatch.");
      return result;
    }

    /// <summary>
    /// Moves to next attribute of the current element.
    /// </summary>
    bool MoveToNextAttribute()
    {
      return reader.MoveToNextAttribute();
    }

    /// <summary>
    /// Moves to next element by skipping all white space.
    /// Returns true if XmlNodeType.Element is the current node type.
    /// </summary>
    bool MoveToNextElement()
    {
      bool success = reader.Read();
      if (success)
      {
        XmlNodeType type = reader.MoveToContent();
        Debug.Assert(type == XmlNodeType.Element || type == XmlNodeType.EndElement || type == XmlNodeType.None);
        success = type == XmlNodeType.Element;
      }
      return success;
    }

    /// <summary>
    /// Moves to first element after the current element with the specified name.
    /// </summary>
    void MoveBeyondThisElement() // string name, int depth)
    {
      if (!reader.IsEmptyElement && reader.NodeType != XmlNodeType.Comment)
      {
        if (reader.NodeType == XmlNodeType.XmlDeclaration)
        {
          MoveToNextElement();
          return;
        }
        else if (reader.NodeType == XmlNodeType.Attribute)
        {
          reader.MoveToElement();
          if (reader.IsEmptyElement)
          {
            MoveToNextElement();
            return;
          }
        }
        MoveToNextElement();
        while (reader.IsStartElement())
          MoveBeyondThisElement();
      }
      MoveToNextElement(); // next element
    }

    //void MoveBeyondElement(string name)
    //{
    //  MoveBeyondElement(name, this.reader.Depth);
    //}

    [Conditional("DEBUG")]
    void AssertElement(string name)
    {
      Debug.Assert(reader.Name == name, PSXSR.UnexpectedElement(reader.Name, name));
    }

    void UnexpectedAttribute(string name)
    {
      Debugger.Break();
      PSXSR.UnexpectedAttribute(name);
    }

    XmlTextReader reader;
    FixedPage fpage;

    ResouceDictionaryStack ResourceDictionaryStack
    {
      get
      {
        if (resourceDictionaryStack == null)
          resourceDictionaryStack = new ResouceDictionaryStack();
        return resourceDictionaryStack;
      }
    }
    ResouceDictionaryStack resourceDictionaryStack;

    internal class ResouceDictionaryStack
    {
      public void Push(ResourceDictionary dic)
      {
        if (stack == null)
          stack = new Stack<ResourceDictionary>();
        stack.Push(dic);
      }

      public ResourceDictionary Pop()
      {
        return stack.Pop();
      }

      public ResourceDictionary Current
      {
        get
        {
          if (stack == null)
            return null;
          if (stack.Count == 0)
            return null;
          return stack.Peek();
        }
      }

      Stack<ResourceDictionary> stack;
    }
  }
}