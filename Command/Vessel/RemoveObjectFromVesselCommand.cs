using System.Text.RegularExpressions;

namespace kOS.Command.Vessel
{
    [AttributeCommand("REMOVE *")]
    public class RemoveObjectFromVesselCommand : Command
    {
        public RemoveObjectFromVesselCommand(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            Expression ex = new Expression(RegexMatch.Groups[1].Value, this);
            object obj = ex.GetValue();

            if (obj is kOS.Node)
            {
                ((Node)obj).Remove();
            }
            else
            {
                throw new kOSException("Supplied object ineligible for removal", this);
            }

            State = ExecutionState.DONE;
        }
    }
}