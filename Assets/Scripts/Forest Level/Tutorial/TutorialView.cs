using UnityEngine;
using UnityEngine.UIElements;

public class TutorialView
{
    readonly Label _title;
    readonly Image _image;
    readonly Label _description;

    TutorialDataSO data;

    public TutorialView(VisualElement container){
        _title = container.Q<Label>("tutorial-title-label");
        _image = container.Q<Image>("tutorial-image");
        _description = container.Q<Label>("tutorial-description-label");
    }

    public void ChangeTutorialInfo(TutorialDataSO data){
        _title.text = data.TutorialName;
        _description.text = data.TutorialDescription;
        _image.style.display = data.tutorialImage? DisplayStyle.Flex: DisplayStyle.None;

        if (data.tutorialImage != null)
            _image.image = data.tutorialImage;
    }
}
