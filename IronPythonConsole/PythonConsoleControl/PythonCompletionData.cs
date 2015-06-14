// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.Scripting.Hosting.Shell;

namespace PythonConsoleControl
{
    /// <summary>
    /// Implements AvalonEdit ICompletionData interface to provide the entries in the completion drop down.
    /// </summary>
    public class PythonCompletionData : ICompletionData
    {
        CommandLine _commandLine;
        
        public PythonCompletionData(string text, string stub, CommandLine commandLine, bool isInstance)
        {
            Text = text;
            Stub = stub;
            _commandLine = commandLine;
            IsInstance = isInstance;
        }

        public ImageSource Image => null;

        public string Text { get; }

        public string Stub { get; private set; }

        public bool IsInstance { get; private set; }

        // Use this property if you want to show a fancy UIElement in the drop down list.
        public object Content => Text;

        public object Description => "Not available";

        public double Priority => 0;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Text);
        }
    }
}
