using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class BaseUIView : IHideableView
{
    readonly VisualElement _container;
    public BaseUIView(VisualElement container){
        _container = container;
    }
    
    public virtual void Hide() => _container.style.display = DisplayStyle.None;
    public virtual void Show() => _container.style.display = DisplayStyle.Flex;
}
