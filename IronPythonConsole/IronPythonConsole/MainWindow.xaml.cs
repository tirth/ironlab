using System;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Scripting;
using Microsoft.Win32;

namespace IronPythonConsole
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        readonly ConsoleOptions _consoleOptionsProvider;
        
        public MainWindow()
		{
            Initialized += MainWindow_Initialized;
            // Load our custom highlighting definition:
            IHighlightingDefinition pythonHighlighting;
            using (var s = typeof(MainWindow).Assembly.GetManifestResourceStream("IronPythonConsole.Resources.Python.xshd"))
            {
                if (s == null)
                    throw new InvalidOperationException("Could not find embedded resource");
                using (XmlReader reader = new XmlTextReader(s))
                    pythonHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
            // and register it in the HighlightingManager
            HighlightingManager.Instance.RegisterHighlighting("Python Highlighting", new[] { ".cool" }, pythonHighlighting);
            	
			InitializeComponent();

            TextEditor.SyntaxHighlighting = pythonHighlighting;

            TextEditor.PreviewKeyDown += textEditor_PreviewKeyDown;

            _consoleOptionsProvider = new ConsoleOptions(Console.Pad);

            PropertyGridComboBox.SelectedIndex = 0;

            Expander.Expanded += expander_Expanded;

            Console.Pad.Host.ConsoleCreated +=Host_ConsoleCreated;
		}

		string _currentFileName;

        void Host_ConsoleCreated(object sender, EventArgs e)
        {
            Console.Pad.Console.ConsoleInitialized += Console_ConsoleInitialized;
        }

        void Console_ConsoleInitialized(object sender, EventArgs e)
        {
            const string startupScipt = "import IronPythonConsole";
            var scriptSource = Console.Pad.Console.ScriptScope.Engine.CreateScriptSourceFromString(startupScipt, SourceCodeKind.Statements);
            try
            {
                scriptSource.Execute();
            }
            catch
            {
                // ignored
            }
            //double[] test = new double[] { 1.2, 4.6 };
            //console.Pad.Console.ScriptScope.SetVariable("test", test);
        }

        static void MainWindow_Initialized(object sender, EventArgs e)
        {
            //propertyGridComboBox.SelectedIndex = 1;
        }
		
		void OpenFileClick(object sender, RoutedEventArgs e)
		{
		    var dlg = new OpenFileDialog {CheckFileExists = true};
		    if (!(dlg.ShowDialog() ?? false)) return;

		    _currentFileName = dlg.FileName;
		    TextEditor.Load(_currentFileName);
		    //textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(currentFileName));
		}
		
		void SaveFileClick(object sender, EventArgs e)
		{
			if (_currentFileName == null) {
			    var dlg = new SaveFileDialog {DefaultExt = ".txt"};
			    if (dlg.ShowDialog() ?? false) {
					_currentFileName = dlg.FileName;
				} else {
					return;
				}
			}
			TextEditor.Save(_currentFileName);
		}

        void RunClick(object sender, EventArgs e)
        {
            RunStatements();
        }

        void textEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5) RunStatements();
        }

        void RunStatements()
        {
            var statementsToRun = "";
            statementsToRun = TextEditor.TextArea.Selection.Length > 0 ? 
                TextEditor.TextArea.Selection.GetText(TextEditor.TextArea.Document) : 
                TextEditor.TextArea.Document.Text;
            Console.Pad.Console.RunStatements(statementsToRun);
        }
		
		void PropertyGridComboBoxSelectionChanged(object sender, RoutedEventArgs e)
		{
            if (PropertyGrid == null)
				return;
			switch (PropertyGridComboBox.SelectedIndex) {
				case 0:
                    PropertyGrid.SelectedObject = _consoleOptionsProvider; // not .Instance
					break;
				case 1:
					//propertyGrid.SelectedObject = textEditor.Options; (for WPF native control)
                    PropertyGrid.SelectedObject = TextEditor.Options;
					break;
			}
		}

        void expander_Expanded(object sender, RoutedEventArgs e)
        {
            PropertyGridComboBoxSelectionChanged(sender, e);
        }
		
    }
}
