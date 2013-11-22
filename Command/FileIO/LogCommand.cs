using System;
using System.Text.RegularExpressions;

namespace kOS
{
    [AttributeCommand("LOG * TO &")]
    public class LogCommand: Command
    {
        public LogCommand(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            // Todo: let the user specify a volume "LOG something TO file ON volume"
            Volume targetVolume = SelectedVolume;

            // If the archive is out of reach, the signal is lost in space.
            if (!targetVolume.CheckRange())
            {
                State = ExecutionState.DONE;
                return;
            }

            String targetFile = RegexMatch.Groups[2].Value.Trim();
            Expression e = new Expression(RegexMatch.Groups[1].Value, ParentContext);

            if (e.IsNull())
            {
                State = ExecutionState.DONE;
            }
            else
            {
                targetVolume.AppendToFile(targetFile, e.ToString());
                State = ExecutionState.DONE;
            }
        }
    }
}