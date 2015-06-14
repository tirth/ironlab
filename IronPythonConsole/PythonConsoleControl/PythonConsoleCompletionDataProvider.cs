// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.AvalonEdit.CodeCompletion;
using IronPython.Runtime;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting.Shell;

namespace PythonConsoleControl
{
    /// <summary>
    /// Provides code completion for the Python Console window.
    /// </summary>
    public class PythonConsoleCompletionDataProvider 
    {
        readonly CommandLine _commandLine;
        internal volatile bool AutocompletionInProgress;

        bool _excludeCallables;
        public bool ExcludeCallables { get { return _excludeCallables; } set { _excludeCallables = value; } }

        public PythonConsoleCompletionDataProvider(CommandLine commandLine)//IMemberProvider memberProvider)
        {
            _commandLine = commandLine;
        }

        /// <summary>
        /// Generates completion data for the specified text. The text should be everything before
        /// the dot character that triggered the completion. The text can contain the command line prompt
        /// '>>>' as this will be ignored.
        /// </summary>
        public ICompletionData[] GenerateCompletionData(string line)
        {         
            var items = new List<PythonCompletionData>(); //DefaultCompletionData

            var name = GetName(line);
            // A very simple test of callables!
            if (_excludeCallables && name.Contains(')')) return null;

            if (string.IsNullOrEmpty(name)) return items.ToArray();
            var stream = _commandLine.ScriptScope.Engine.Runtime.IO.OutputStream;
            try
            {
                AutocompletionInProgress = true;
                // Another possibility:
                //commandLine.ScriptScope.Engine.Runtime.IO.SetOutput(new System.IO.MemoryStream(), Encoding.UTF8);
                //object value = commandLine.ScriptScope.Engine.CreateScriptSourceFromString(name, SourceCodeKind.Expression).Execute(commandLine.ScriptScope);
                //IList<string> members = commandLine.ScriptScope.Engine.Operations.GetMemberNames(value);
                var type = TryGetType(name);
                // Use Reflection for everything except in-built Python types and COM pbjects. 
                if (type != null && type.Namespace != "IronPython.Runtime" && (type.Name != "__ComObject"))
                {
                    PopulateFromClrType(items, type, name);
                }
                else
                {
                    var dirCommand = "dir(" + name + ")";
                    object value = _commandLine.ScriptScope.Engine.CreateScriptSourceFromString(dirCommand, SourceCodeKind.Expression).Execute(_commandLine.ScriptScope);
                    AutocompletionInProgress = false;
                    if (value != null)
                        items.AddRange((value as List).Select(member => new PythonCompletionData((string) member, name, _commandLine, false)));
                }
            }
            catch (ThreadAbortException tae)
            {
                if (tae.ExceptionState is KeyboardInterruptException) Thread.ResetAbort();
            }
            catch
            {
                // Do nothing.
            }
            _commandLine.ScriptScope.Engine.Runtime.IO.SetOutput(stream, Encoding.UTF8);
            AutocompletionInProgress = false;
            return items.ToArray();
        }

        protected Type TryGetType(string name)
        {
            var tryGetType = name + ".GetType()";
            object type = null;
            try
            {
                type = _commandLine.ScriptScope.Engine.CreateScriptSourceFromString(tryGetType, SourceCodeKind.Expression).Execute(_commandLine.ScriptScope);
            }
            catch (ThreadAbortException tae)
            {
                if (tae.ExceptionState is KeyboardInterruptException) Thread.ResetAbort();
            }
            catch
            {
                // Do nothing.
            }
            return type as Type;
        }

        protected void PopulateFromClrType(List<PythonCompletionData> items, Type type, string name)
        {
            var methodInfo = type.GetMethods();
            var propertyInfo = type.GetProperties();
            var fieldInfo = type.GetFields();
            var completionsList = (methodInfo.Where(
                methodInfoItem =>
                    (methodInfoItem.IsPublic) && (methodInfoItem.Name.IndexOf("get_", StringComparison.Ordinal) != 0) &&
                    (methodInfoItem.Name.IndexOf("set_", StringComparison.Ordinal) != 0) &&
                    (methodInfoItem.Name.IndexOf("add_", StringComparison.Ordinal) != 0) &&
                    (methodInfoItem.Name.IndexOf("remove_", StringComparison.Ordinal) != 0) &&
                    (methodInfoItem.Name.IndexOf("__", StringComparison.Ordinal) != 0))
                .Select(methodInfoItem => methodInfoItem.Name)).ToList();
            completionsList.AddRange(propertyInfo.Select(propertyInfoItem => propertyInfoItem.Name));
            completionsList.AddRange(fieldInfo.Select(fieldInfoItem => fieldInfoItem.Name));
            completionsList.Sort();
            var last = "";
            for (var i = completionsList.Count - 1; i > 0; --i)
            {
                if (completionsList[i] == last) completionsList.RemoveAt(i);
                else last = completionsList[i];
            }
            items.AddRange(completionsList.Select(completion => new PythonCompletionData(completion, name, _commandLine, true)));
        }

        /// <summary>
        /// Generates completion data for the specified text. The text should be everything before
        /// the dot character that triggered the completion. The text can contain the command line prompt
        /// '>>>' as this will be ignored.
        /// </summary>
        public void GenerateDescription(string stub, string item, DescriptionUpdateDelegate updateDescription, bool isInstance)
        {
            var stream = _commandLine.ScriptScope.Engine.Runtime.IO.OutputStream;
            var description = "";
            if (string.IsNullOrEmpty(item)) return;
            try
            {
                AutocompletionInProgress = true;
                // Another possibility:
                //commandLine.ScriptScope.Engine.Runtime.IO.SetOutput(new System.IO.MemoryStream(), Encoding.UTF8);
                //object value = commandLine.ScriptScope.Engine.CreateScriptSourceFromString(item, SourceCodeKind.Expression).Execute(commandLine.ScriptScope);
                //description = commandLine.ScriptScope.Engine.Operations.GetDocumentation(value);
                var docCommand = "";
                if (isInstance) docCommand = "type(" + stub + ")" + "." + item + ".__doc__";
                else docCommand = stub + "." + item + ".__doc__";
                object value = _commandLine.ScriptScope.Engine.CreateScriptSourceFromString(docCommand, SourceCodeKind.Expression).Execute(_commandLine.ScriptScope);
                description = (string)value;
                AutocompletionInProgress = false;
            }
            catch (ThreadAbortException tae)
            {
                if (tae.ExceptionState is KeyboardInterruptException) Thread.ResetAbort();
                AutocompletionInProgress = false;
            }
            catch
            {
                AutocompletionInProgress = false;
                // Do nothing.
            }
            _commandLine.ScriptScope.Engine.Runtime.IO.SetOutput(stream, Encoding.UTF8);
            updateDescription(description);
        }


        static string GetName(string text)
        {
            text = text.Replace("\t", "   ");
            var startIndex = text.LastIndexOf(' ');
            return text.Substring(startIndex + 1).Trim('.');
        }

    }
}
