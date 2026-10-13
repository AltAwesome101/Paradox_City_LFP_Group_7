using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class BaseUIView : IHideableView
{
    protected readonly VisualElement _container;
    public BaseUIView(VisualElement container){
        _container = container;
    }
    
    public virtual void Hide() => _container.style.display = DisplayStyle.None;
    public virtual void Show() => _container.style.display = DisplayStyle.Flex;
}


public class InteractPromptView : BaseUIView
{
    readonly Label _label;
    public InteractPromptView(VisualElement container) : base(container)
        => _label = container.Q<Label>("interactPrompt-label");
    public void SetText(string t) => _label.text = t;
}