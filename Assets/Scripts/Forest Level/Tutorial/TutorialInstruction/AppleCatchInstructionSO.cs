using UnityEngine;

[CreateAssetMenu(fileName = "AppleCatchInstructionSO", menuName = "TutorialInstructions/AppleCatch")]
public class AppleCatchInstructionSO : BaseTutorialInstructionSO
{
    public override IPredicate CreateCondition(InputReaderSO input) => new VoidPredicate();
}