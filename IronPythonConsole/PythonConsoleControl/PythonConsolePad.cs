// Copyright (c) 2010 Joe Moorhouse

using System.Windows.Media;
using ICSharpCode.AvalonEdit;

namespace PythonConsoleControl
{   
    public class PythonConsolePad 
    {
        readonly PythonTextEditor _pythonTextEditor;
        readonly TextEditor _textEditor;
        readonly PythonConsoleHost _host;

        public PythonConsolePad()
        {
            _textEditor = new TextEditor();
            _pythonTextEditor = new PythonTextEditor(_textEditor);
            _host = new PythonConsoleHost(_pythonTextEditor);
            _host.Run();
            _textEditor.FontFamily = new FontFamily("Consolas");
            _textEditor.FontSize = 12;
        }

        public TextEditor Control => _textEditor;

        public PythonConsoleHost Host => _host;

        public PythonConsole Console => _host.Console;

        public void Dispose()
        {
            _host.Dispose();
        }
    }
}
