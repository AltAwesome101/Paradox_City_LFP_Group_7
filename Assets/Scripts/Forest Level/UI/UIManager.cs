using UnityEngine;
using BetterSingletons;
using System.Collections.Generic;

public class UIManager : Singleton<UIManager> 
{
    List<IHideableView> ExplorativeUIList = new();
    List<IHideableView> ObjectiveUIList = new();
    List<IHideableView> TutorialUIList = new();




}
