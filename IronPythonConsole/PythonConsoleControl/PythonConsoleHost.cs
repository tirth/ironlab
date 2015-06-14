// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Linq;
using System.Text;
using System.Threading;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.Scripting.Hosting.Shell;

namespace PythonConsoleControl
{
    public delegate void ConsoleCreatedEventHandler(object sender, EventArgs e);

    /// <summary>
    /// Hosts the python console.
    /// </summary>
    public class PythonConsoleHost : ConsoleHost, IDisposable
    {
        Thread _thread;
        readonly PythonTextEditor _textEditor;

        public event ConsoleCreatedEventHandler ConsoleCreated;

        public PythonConsoleHost(PythonTextEditor textEditor)
        {
            _textEditor = textEditor;
        }

        public PythonConsole Console { get; private set; }

        protected override Type Provider => typeof(PythonContext);

        /// <summary>
        /// Runs the console host in its own thread.
        /// </summary>
        public void Run()
        {
            _thread = new Thread(RunConsole) {IsBackground = true};
            _thread.Start();
        }

        public void Dispose()
        {
            Console?.Dispose();

            _thread?.Join();
        }

        protected override CommandLine CreateCommandLine()
        {
            return new PythonCommandLine();
        }

        protected override OptionsParser CreateOptionsParser()
        {
            return new PythonOptionsParser();
        }

        /// <remarks>
        /// After the engine is created the standard output is replaced with our custom Stream class so we
        /// can redirect the stdout to the text editor window.
        /// This can be done in this method since the Runtime object will have been created before this method
        /// is called.
        /// </remarks>
        protected override IConsole CreateConsole(ScriptEngine engine, CommandLine commandLine, ConsoleOptions options)
        {
            SetOutput(new PythonOutputStream(_textEditor));
            Console = new PythonConsole(_textEditor, commandLine);
            ConsoleCreated?.Invoke(this, EventArgs.Empty);
            return Console;
        }

        protected virtual void SetOutput(PythonOutputStream stream)
        {
            Runtime.IO.SetOutput(stream, Encoding.UTF8);
        }

        /// <summary>
        /// Runs the console.
        /// </summary>
        void RunConsole()
        {
            Run(new[] { "-X:FullFrames" });
        }

        protected override ScriptRuntimeSetup CreateRuntimeSetup()
        {
            var srs = ScriptRuntimeSetup.ReadConfiguration();
            foreach (var langSetup in srs.LanguageSetups.Where(langSetup => langSetup.FileExtensions.Contains(".py")))
                langSetup.Options["SearchPaths"] = new string[0];
            return srs;
        }

        protected override void ParseHostOptions(string[] args)
        {
            // Python doesn't want any of the DLR base options.
            foreach (var s in args)
            {
                Options.IgnoredArgs.Add(s);
            }
        }

        protected override void ExecuteInternal()
        {
            var pc = HostingHelpers.GetLanguageContext(Engine) as PythonContext;
            pc?.SetModuleState(typeof(ScriptEngine), Engine);
            base.ExecuteInternal();
        }
    }
}
