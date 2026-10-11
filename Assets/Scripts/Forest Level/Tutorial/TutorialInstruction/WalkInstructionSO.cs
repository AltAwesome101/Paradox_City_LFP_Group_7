using UnityEngine;

[CreateAssetMenu(fileName = "WalkInstructionSO", menuName = "TutorialInstructions/Walk")]
public class WalkInstructionSO : BaseTutorialInstructionSO
{
    [SerializeField] float holdThreshold = .25f;
    public override IPredicate CreateCondition(InputReaderSO input) => new HeldPredicate(() => input.MoveDirection != Vector2.zero,holdThreshold);

}
