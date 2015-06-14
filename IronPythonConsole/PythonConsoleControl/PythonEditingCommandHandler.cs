// Copyright (c) 2010 Joe Moorhouse

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;

namespace PythonConsoleControl
{
    /// <summary>
    /// Commands that only involve the text editor are outsourced to here.
    /// </summary>
    class PythonEditingCommandHandler
    {
        PythonTextEditor _textEditor;
        TextArea _textArea;
        
        public PythonEditingCommandHandler(PythonTextEditor textEditor)
        {
            _textEditor = textEditor;
            _textArea = textEditor.TextArea;
        }

        internal static void CanCutOrCopy(object target, CanExecuteRoutedEventArgs args)
        {
            // HasSomethingSelected for copy and cut commands
            var textArea = GetTextArea(target);
            if (textArea != null && textArea.Document != null)
            {
                args.CanExecute = textArea.Options.CutCopyWholeLine || !textArea.Selection.IsEmpty;
                args.Handled = true;
            }
        }

        internal static TextArea GetTextArea(object target)
        {
            return target as TextArea;
        }

        internal static void OnCopy(object target, ExecutedRoutedEventArgs args)
        {
            var textArea = GetTextArea(target);
            if (textArea != null && textArea.Document != null)
            {
                if (textArea.Selection.IsEmpty && textArea.Options.CutCopyWholeLine)
                {
                    var currentLine = textArea.Document.GetLineByNumber(textArea.Caret.Line);
                    CopyWholeLine(textArea, currentLine);
                }
                else
                {
                    CopySelectedText(textArea);
                }
                args.Handled = true;
            }
        }

        internal static void OnCut(object target, ExecutedRoutedEventArgs args)
        {
            var textArea = GetTextArea(target);
            if (textArea != null && textArea.Document != null)
            {
                if (textArea.Selection.IsEmpty && textArea.Options.CutCopyWholeLine)
                {
                    var currentLine = textArea.Document.GetLineByNumber(textArea.Caret.Line);
                    CopyWholeLine(textArea, currentLine);
                    textArea.Document.Remove(currentLine.Offset, currentLine.TotalLength);
                }
                else
                {
                    CopySelectedText(textArea);
                    textArea.Selection.ReplaceSelectionWithText(textArea, string.Empty);
                }
                textArea.Caret.BringCaretToView();
                args.Handled = true;
            }
        }

        internal static void CopySelectedText(TextArea textArea)
        {
            var data = textArea.Selection.CreateDataObject(textArea);

            try
            {
                Clipboard.SetDataObject(data, true);
            }
            catch (ExternalException)
            {
                // Apparently this exception sometimes happens randomly.
                // The MS controls just ignore it, so we'll do the same.
                return;
            }

            var text = textArea.Selection.GetText(textArea.Document);
            text = TextUtilities.NormalizeNewLines(text, Environment.NewLine);
            //textArea.OnTextCopied(new TextEventArgs(text));
        }

        internal static void CopyWholeLine(TextArea textArea, DocumentLine line)
        {
            ISegment wholeLine = new VerySimpleSegment(line.Offset, line.TotalLength);
            var text = textArea.Document.GetText(wholeLine);
            // Ensure we use the appropriate newline sequence for the OS
            text = TextUtilities.NormalizeNewLines(text, Environment.NewLine);
            var data = new DataObject(text);

            // Also copy text in HTML format to clipboard - good for pasting text into Word
            // or to the SharpDevelop forums.
            var highlighter = textArea.GetService(typeof(IHighlighter)) as IHighlighter;
            HtmlClipboard.SetHtml(data, HtmlClipboard.CreateHtmlFragment(textArea.Document, highlighter, wholeLine, new HtmlOptions(textArea.Options)));

            var lineSelected = new MemoryStream(1);
            lineSelected.WriteByte(1);
            data.SetData(LineSelectedType, lineSelected, false);

            try
            {
                Clipboard.SetDataObject(data, true);
            }
            catch (ExternalException)
            {
                // Apparently this exception sometimes happens randomly.
                // The MS controls just ignore it, so we'll do the same.
            }
            //textArea.OnTextCopied(new TextEventArgs(text));
        }

        internal static ExecutedRoutedEventHandler OnDelete(RoutedUICommand selectingCommand)
        {
            return (target, args) =>
            {
                var textArea = GetTextArea(target);
                if (textArea != null && textArea.Document != null)
                {
                    // call BeginUpdate before running the 'selectingCommand'
                    // so that undoing the delete does not select the deleted character
                    using (textArea.Document.RunUpdate())
                    {
                        var textAreaType = textArea.GetType();
                        MethodInfo method;
                        if (textArea.Selection.IsEmpty)
                        {
                            var oldCaretPosition = textArea.Caret.Position;
                            selectingCommand.Execute(args.Parameter, textArea);
                            var hasSomethingDeletable = false;
                            foreach (var s in textArea.Selection.Segments)
                            {
                                method = textAreaType.GetMethod("GetDeletableSegments", BindingFlags.Instance | BindingFlags.NonPublic); 
                                //textArea.GetDeletableSegments(s).Length > 0)
                                if ((int)method.Invoke(textArea, new Object[]{s}) > 0) 
                                {
                                    hasSomethingDeletable = true;
                                    break;
                                }
                            }
                            if (!hasSomethingDeletable)
                            {
                                // If nothing in the selection is deletable; then reset caret+selection
                                // to the previous value. This prevents the caret from moving through read-only sections.
                                textArea.Caret.Position = oldCaretPosition;
                                textArea.Selection = Selection.Empty;
                            }
                        }
                        method = textAreaType.GetMethod("RemoveSelectedText", BindingFlags.Instance | BindingFlags.NonPublic);
                        method.Invoke(textArea, new Object[]{});
                        //textArea.RemoveSelectedText();
                    }
                    textArea.Caret.BringCaretToView();
                    args.Handled = true;
                }
            };
        }

        internal static void CanDelete(object target, CanExecuteRoutedEventArgs args)
        {
            // HasSomethingSelected for delete command
            var textArea = GetTextArea(target);
            if (textArea != null && textArea.Document != null)
            {
                args.CanExecute = !textArea.Selection.IsEmpty;
                args.Handled = true;
            }
        }

        const string LineSelectedType = "MSDEVLineSelect";  // This is the type VS 2003 and 2005 use for flagging a whole line copy

        struct VerySimpleSegment : ISegment
	    {
		    public readonly int Offset, Length;
		
		    int ISegment.Offset => Offset;

            int ISegment.Length => Length;

            public int EndOffset => Offset + Length;

            public VerySimpleSegment(int offset, int length)
		    {
			    Offset = offset;
			    Length = length;
		    }
        }
    }
}
