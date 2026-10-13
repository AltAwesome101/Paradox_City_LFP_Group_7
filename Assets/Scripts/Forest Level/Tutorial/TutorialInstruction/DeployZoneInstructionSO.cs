using UnityEngine;

[CreateAssetMenu(fileName = "DeployZoneInstructionSO", menuName = "TutorialInstructions/DeployZone")]
public class DeployZoneInstructionSO : BaseTutorialInstructionSO
{
    public override IPredicate CreateCondition(InputReaderSO input) => new VoidPredicate();

}
