using UnityEngine.UIElements;

namespace Forestlevel
{
    public class AppleCollectionView : BaseUIView
    {
        readonly Label _collectionLabel;

        public AppleCollectionView(VisualElement container)
            : base(container)
        {
            _collectionLabel = container.Q<Label>("appleCollection-label");
            Hide();
        }

        public void UpdateDisplay(string display)=> _collectionLabel.text = display;
    }
}