using UnityEngine.UIElements;

public class ObjectiveView : BaseUIView
{
    readonly Label _objectiveLabel;
    public ObjectiveView(VisualElement container) : base(container)
    {
        _objectiveLabel = container.Q<Label>("objective-label");
    }

    public void UpdateDisplay(string display)=> _objectiveLabel.text = display;

}