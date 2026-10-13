using UnityEngine;
using UnityEngine.UIElements;

public class TutorialView : BaseUIView
{
    readonly Label _instruction;
    readonly Image _image;

    public TutorialView(VisualElement container) : base(container)
    {
        _instruction = container.Q<Label>("tutorial-instruction-label");
        _image = container.Q<Image>("tutorial-image");

        container.pickingMode = PickingMode.Ignore;   // prompt must never block the screen
        // _image.scaleMode = ScaleMode.ScaleToFit;
    }

    public void SetTutorialDisplay(BaseTutorialInstructionSO tutorial)
    {
        _instruction.text = tutorial.Instruction;

        var image = tutorial.InstructionImage;
        _image.image = image;
        _image.style.display = image != null ? DisplayStyle.Flex : DisplayStyle.None;
    }
}