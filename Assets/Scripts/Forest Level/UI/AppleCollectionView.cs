using UnityEngine;
using UnityEngine.UIElements;
namespace Forestlevel
{
    public class AppleCollectionView
    {
        readonly VisualElement _container;
        readonly Label _collectionLabel;

        public AppleCollectionView(VisualElement container){
            _container = container;
            _collectionLabel = container.Q<Label>("appleCollection-label");
            Hide();
        }

        public void Hide() => _container.style.display = DisplayStyle.None;
        public void Show()=> _container.style.display = DisplayStyle.Flex;
        public void UpdateDisplay(string display) => _collectionLabel.text = display;
    }
    
}
