using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField, Tooltip("The scene to enter when starting a new game.")] private string newGameScene;
    [SerializeField, Tooltip("The game menu controller.")] private GameMenuController gameMenuController;

    private void Start()
    {
        GameManager.Instance.AudioManager.PlayMusic("Titlescreen");
    }

    public void NewGame() => GameManager.Instance.LoadScene(newGameScene);
    public void Load() => gameMenuController.ToggleGameMenu(GameMenuController.SubMenu.Load);
    public void Options() => gameMenuController.ToggleGameMenu(GameMenuController.SubMenu.Options);
    public void Compendium() => gameMenuController.ToggleGameMenu(GameMenuController.SubMenu.Compendium);
    public void Credits() => gameMenuController.ToggleGameMenu(GameMenuController.SubMenu.Credits);
    public void Exit() => GameManager.Instance.QuitGame();
}
