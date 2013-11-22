using System.Text.RegularExpressions;

namespace kOS.Command.Temporal
{
    [AttributeCommand("UNLOCK %")]
    public class UnlockCommand : Command
    {
        public UnlockCommand(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var varname = RegexMatch.Groups[1].Value;

            if (varname.ToUpper() == "ALL")
            {
                ParentContext.UnlockAll();
            }
            else
            {
                ParentContext.Unlock(varname);
            }

            State = ExecutionState.DONE;
        }
    }
}