using UnityEngine;
using UnityEngine.UIElements;

public class TutorialView
{
    // Tutorial Display
    readonly VisualElement _container;
    readonly Label _title;
    readonly Image _image;
    readonly Label _description;

    // Button controls
    readonly Button _closeButton;
    readonly Button _previousButton;
    readonly Button _nextButton;

    // Event handling
    public event System.Action CloseRequested;
    public event System.Action PreviousRequested;
    public event System.Action NextRequested;

    public TutorialView(VisualElement container)
    {
        _container = container;

        _title = container.Q<Label>("tutorial-title-label");
        _image = container.Q<Image>("tutorial-image");
        _description = container.Q<Label>("tutorial-description-label");

        _closeButton = container.Q<Button>("tutorial-close-button");
        _previousButton = container.Q<Button>("tutorial-prev-button");
        _nextButton = container.Q<Button>("tutorial-next-button");

        RegisterCallbacks();
    }

    void RegisterCallbacks()
    {
        _closeButton.clicked += OnCloseButtonPressed;
        _previousButton.clicked += OnPreviousButtonPressed;
        _nextButton.clicked += OnNextButtonPressed;
    }

    void DeregisterCallbacks()
    {
        _closeButton.clicked -= OnCloseButtonPressed;
        _previousButton.clicked -= OnPreviousButtonPressed;
        _nextButton.clicked -= OnNextButtonPressed;
    }

        
    void OnCloseButtonPressed() => CloseRequested?.Invoke();
    void OnPreviousButtonPressed() => PreviousRequested?.Invoke();
    void OnNextButtonPressed() => NextRequested?.Invoke();

    public void HideTutorialPanel()=> _container.style.display = DisplayStyle.None;
    public void EnableTutorialPanel()=> _container.style.display = DisplayStyle.Flex;

    public void ChangeTutorialInfo(TutorialDataSO data)
    {
        EnableTutorialPanel();

        _title.text = data.TutorialName;
        _description.text = data.TutorialDescription;

        _image.style.display = data.tutorialImage? DisplayStyle.Flex: DisplayStyle.None;

        if (data.tutorialImage != null)
            _image.image = data.tutorialImage;
    }
}