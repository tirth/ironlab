﻿// Copyright (c) 2010 Joe Moorhouse

using System;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace PythonConsoleControl
{
    /// <summary>
    /// Only colourize when text is input
    /// </summary>
    public class PythonConsoleHighlightingColorizer : HighlightingColorizer
    {
        readonly TextDocument _document;

        /// <summary>
        /// Creates a new HighlightingColorizer instance.
        /// </summary>
        /// <param name="ruleSet">The root highlighting rule set.</param>
        public PythonConsoleHighlightingColorizer(HighlightingRuleSet ruleSet, TextDocument document)
            : base(ruleSet)
        {
            if (document == null)
                throw new ArgumentNullException("document");
            _document = document;
        }

        /// <inheritdoc/>
        protected override void ColorizeLine(DocumentLine line)
        {
            var highlighter = CurrentContext.TextView.Services.GetService(typeof(IHighlighter)) as IHighlighter;
            var lineString = _document.GetText(line);
            if (highlighter != null)
            {
                if (lineString.Length < 3 || lineString.Substring(0, 3) == ">>>" || lineString.Substring(0, 3) == "...") {
                    var hl = highlighter.HighlightLine(line.LineNumber);
                    foreach (var section in hl.Sections)
                    {
                        ChangeLinePart(section.Offset, section.Offset + section.Length,
                                       visualLineElement => ApplyColorToElement(visualLineElement, section.Color));
                    }
                }
            }
        }
    }
}
