using BetterSingletons;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class TutorialController : Singleton<TutorialController>
{
    [SerializeField] List<TutorialDataSO> startingData;
    [SerializeField] UIDocument document;

    TutorialView _view;

    List<TutorialDataSO> _tutorials;
    int _currentIndex;

    protected override void Awake()
    {
        base.Awake();

        var root = document.rootVisualElement;

        _tutorials = new List<TutorialDataSO>(startingData);

        _view = new TutorialView(container: root.Q<VisualElement>("TutorialPanel-container"));

        RegisterViewCallbacks();
    }

    void Start()=> OpenTutorials();

    void RegisterViewCallbacks()
    {
        _view.CloseRequested += CloseTutorial;
        _view.PreviousRequested += ShowPreviousTutorial;
        _view.NextRequested += ShowNextTutorial;
    }

    public void OpenTutorials()
    {
        _currentIndex = 0;
        DisplayCurrentTutorial();
    }

    void DisplayCurrentTutorial()
    {
        TutorialDataSO data = _tutorials[_currentIndex];
        _view.ChangeTutorialInfo(data);
    }

    void ShowNextTutorial()
    {
        if (_currentIndex >= _tutorials.Count - 1)
            return;

        _currentIndex++;
        DisplayCurrentTutorial();
    }

    void ShowPreviousTutorial()
    {
        if (_currentIndex <= 0) return;

        _currentIndex--;
        DisplayCurrentTutorial();
    }

    void CloseTutorial() => _view.HideTutorialPanel();
}