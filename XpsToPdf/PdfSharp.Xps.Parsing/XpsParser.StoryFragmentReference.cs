﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;
using System.IO;
using PdfSharp.Xps.XpsModel;

namespace PdfSharp.Xps.Parsing
{
  partial class XpsParser
  {
    /// <summary>
    /// Parses a StoryFragmentReference element.
    /// </summary>
    StoryFragmentReference ParseStoryFragmentReference()
    {
      Debug.Assert(reader.Name == "");
      bool isEmptyElement = reader.IsEmptyElement;
      StoryFragmentReference storyFragmentReference = new StoryFragmentReference();
      while (MoveToNextAttribute())
      {
        switch (reader.Name)
        {
          case "Page":
            storyFragmentReference.Page = int.Parse(reader.Value);
            break;

          case "FragmentName":
            storyFragmentReference.FragmentName = reader.Value;
            break;

          default:
            UnexpectedAttribute(reader.Name);
            break;
        }
      }
      MoveToNextElement();
      return storyFragmentReference;
    }
  }
}