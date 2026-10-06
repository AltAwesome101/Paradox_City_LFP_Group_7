using UnityEngine;
using UnityEngine.UIElements;
using GameDevExtensionMethods;
using BetterEventBus;
using Forestlevel;
using UnityEngine.SceneManagement;

public class PauseMenuView : BaseUIView
{
    readonly VisualElement _container;
    readonly Button _restartbtn;
    readonly Button _resumebtn;
    readonly Button _futurebtn;

    //Scene Manager
    SceneReference currentScene;
    SceneReference futureScene;

    
    public PauseMenuView(VisualElement container,SceneReference CurrentScene,SceneReference FutureScene): base(container){
        _container = container;
        _restartbtn = container.Q<Button>("restart-button");
        _resumebtn = container.Q<Button>("resume-button");
        _futurebtn = container.Q<Button>("future-button");

        // Scene assignment
        currentScene = CurrentScene;
        futureScene = FutureScene;

        _restartbtn.clicked += RestartLevel;
        _resumebtn.clicked += Hide;
        _futurebtn.clicked += ReturnToFuture;
    }

    public  override void Hide(){
        base.Hide();
        _container.SetDisplay(false);
        Time.timeScale = 1f;
        CursorUtility.LockAndHide(visible: false,lockstate: true);
    }
    public override void Show(){
        base.Show();
        _container.SetDisplay(true);
        Time.timeScale = 0f;
        CursorUtility.LockAndHide(visible: true,lockstate: false);
    }

    void RestartLevel(){
        Debug.Log("Restart Level");
        Hide();
        SceneManager.LoadScene(currentScene.Name);
    }
    void ReturnToFuture(){
        Debug.Log("Return to the future");
        SceneManager.LoadScene(futureScene.Name);
    }

    public void Dispose(){
        _restartbtn.clicked -= RestartLevel;
        _resumebtn.clicked -= Hide;
        _futurebtn.clicked -= ReturnToFuture;
    }


}
