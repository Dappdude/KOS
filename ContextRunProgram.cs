﻿using System;
using System.Collections.Generic;
using System.Linq;


namespace kOS
{
    public class ContextRunProgram : ExecutionContext
    {
        private File file;
        private String commandBuffer;
        private readonly List<Command.Command> commands = new List<Command.Command>();
        private readonly List<Expression> parameters = new List<Expression>();
        private const int EXECUTION_LINE = 0;


        public string Filename;

        public ContextRunProgram(ExecutionContext parent, List<Expression> parameters, String filename) : base(parent) 
        {
            this.parameters = parameters;
            Filename = filename;
        }

        public void Run(File toRun)
        {
            file = toRun;

            State = ExecutionState.WAIT;

            RunBlock(toRun);
        }

        private void RunBlock(IEnumerable<string> block)
        {
            foreach (var line in block.Select(StripComment))
            {
                commandBuffer += line + "\n";
            }

            string cmd;
            var lineNumber = 0;
            int commandLineStart;
            while (parseNext(ref commandBuffer, out cmd, ref lineNumber, out commandLineStart))
            {
                try
                {
                    Line = commandLineStart;
                    var cmdObj = Command.Command.Get(cmd, this, commandLineStart);
                    commands.Add(cmdObj);
                }
                catch (kOSException e)
                {
                    if (ParentContext.FindClosestParentOfType<ContextRunProgram>() != null)
                    {
                        // Error occurs in a child of another running program
                        StdOut("Error in '" + e.Program.Filename + "' on line " + e.LineNumber + ": " + e.Message);
                        State = ExecutionState.DONE;
                        return;
                    }
                    // Error occurs in the top level program
                    StdOut("Error on line " + e.LineNumber + ": " + e.Message);
                    State = ExecutionState.DONE;
                    return;
                }
                catch (Exception e)
                {
                    // Non-kos exception! This is a bug, but no reason to kill the OS
                    StdOut("Flagrant error on line " + lineNumber);
                    UnityEngine.Debug.Log("Program error");
                    UnityEngine.Debug.Log(e);
                    State = ExecutionState.DONE;
                    return;
                }
            }

            if (commandBuffer.Trim() == "") return;
            StdOut("End of file reached inside unterminated statement");
            State = ExecutionState.DONE;
        }

        public override bool Break()
        {
            State = ExecutionState.DONE;

            return true;
        }

        public string StripComment(string line)
        {
            for (var i=0; i<line.Length; i++)
            {
                if (line[i] == '\"')
                {
                    i = Utils.FindEndOfString(line, i + 1);
                    if (i == -1) break;
                }
                else if (i < line.Length - 1 && line.Substring(i, 2) == "//")
                {
                    return line.Substring(0, i);
                }
            }

            return line;
        }
        
        public override void Update(float time)
        {
            try
            {
                base.Update(time);
                EvaluateNextCommand();
            }
            catch (kOSException e)
            {
                if (ParentContext.FindClosestParentOfType<ContextRunProgram>() != null)
                {
                    // Error occurs in a child of another running program
                    StdOut("Error in '" + e.Program.Filename + "' on line " + e.LineNumber + ": " + e.Message);
                    State = ExecutionState.DONE;
                    return;
                }
                // Error occurs in the top level program
                StdOut("Error on line " + e.LineNumber + ": " + e.Message);
                State = ExecutionState.DONE;
            }
            catch (Exception e)
            {
                // Non-kos exception! This is a bug, but no reason to kill the OS
                StdOut("Flagrant error on line " + EXECUTION_LINE);
                UnityEngine.Debug.Log("Program error");
                UnityEngine.Debug.Log(e);
                State = ExecutionState.DONE;
            }
        }

        private void EvaluateNextCommand()
        {
            if (ChildContext != null) return;
            if (commands.Count > 0)
            {
                Command.Command cmd = commands[0];
                commands.RemoveAt(0);

                ChildContext = cmd;
                cmd.Evaluate();
            }
            else
            {
                State = ExecutionState.DONE;
            }
        }

        public object PopParameter()
        {
            if (parameters.Count > 0)
            {
                object retValue = parameters[0].GetValue();
                parameters.RemoveAt(0);

                return retValue;
            }

            throw new kOSException("Wrong number of parameters supplied");
        }
    }
}
