using UnityEngine;
using UnityEngine.UIElements;

public class TutorialView : BaseUIView
{
    readonly Label _instruction;
    readonly Image _icon;

    public TutorialView(VisualElement container) : base(container)
    {
        _instruction = container.Q<Label>("tutorial-instruction-label");
        // _icon        = container.Q<Image>("tutorial-image");

        container.pickingMode = PickingMode.Ignore;   // prompt must never block the screen
        // _icon.scaleMode = ScaleMode.ScaleToFit;
    }

    public void SetTutorialDisplay(BaseTutorialInstructionSO tutorial)
    {
        _instruction.text = tutorial.Instruction;

        // var icon = tutorial.InstructionIcon;
        // _icon.image = icon;
        // _icon.style.display = icon != null ? DisplayStyle.Flex : DisplayStyle.None;
    }
}