using System.Collections;
using UnityEngine;

public class GameMenuController : MonoBehaviour
{
    [SerializeField, Tooltip("The amount of time to fade in / out the game menu.")] private float fadeDuration = 0.5f;
    [Space()]
    [SerializeField, Tooltip("The sub-menus in the game menu.")] private GameObject[] subMenus;

    private PlayerControls playerControls;
    private CanvasGroup canvasGroup;

    private bool animationActive;
    private GameObject currentSubMenu;

    public enum SubMenu { Transcript, Save, Load, Options, Compendium, Credits };

    private void Awake()
    {
        playerControls = new PlayerControls();
        playerControls.Player.Pause.performed += _ => GetPlayerInput();

        canvasGroup = GetComponent<CanvasGroup>();

        //Resets the game menu values
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        GameManager.Instance.InGameMenu = false;
        animationActive = false;

        //Hide all menus
        foreach (var menu in subMenus)
            menu.SetActive(false);
        currentSubMenu = null;
    }

    private void OnEnable()
    {
        playerControls?.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Disable();
    }

    /// <summary>
    /// Gets the player's input.
    /// </summary>
    private void GetPlayerInput()
    {
        //Toggle the game menu if this isn't the title screen
        if (!GameManager.Instance.InGameMenu && GameManager.Instance.GetSceneName() != "Title")
            ToggleGameMenu();
        else if (GameManager.Instance.InGameMenu)
            ToggleGameMenu();
    }

    /// <summary>
    /// Toggles the visibility of the game menu.
    /// </summary>
    /// <param name="startingMenu">The submenu to open the game menu on.</param>
    public void ToggleGameMenu(SubMenu startingMenu = SubMenu.Save)
    {
        //If the game menu is animating, return
        if (animationActive)
            return;

        //Pause the game and show the game menu
        if (!GameManager.Instance.InGameMenu)
        {
            SwitchToMenu(startingMenu);
            StartCoroutine(FadeGameMenu(0f, 1f));
            Time.timeScale = 0f;
        }
        //Resume the game
        else
        {
            StartCoroutine(FadeGameMenu(1f, 0f));
            Time.timeScale = 1f;
        }
    }

    private IEnumerator FadeGameMenu(float startingAlpha, float endingAlpha)
    {
        animationActive = true;
        float fadeElapsed = 0f;
        canvasGroup.alpha = startingAlpha;

        //If the game menu is going to fade in, take a screenshot first
        if(GameManager.Instance.GetSceneName() != "Title" && endingAlpha == 1f)
            yield return GameManager.Instance.ScreenshotGame();

        //Lerp the alpha of the canvas
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startingAlpha, endingAlpha, fadeElapsed / fadeDuration);
            yield return null;
        }

        //Finalize the values of the alpha
        canvasGroup.alpha = endingAlpha;
        canvasGroup.blocksRaycasts = canvasGroup.alpha > 0f;
        GameManager.Instance.InGameMenu = canvasGroup.alpha > 0f;
        animationActive = false;

        //If the game is no longer in the menu, hide the sub menus
        if (!GameManager.Instance.InGameMenu)
        {
            //Hide all menus
            foreach (var menu in subMenus)
                menu.SetActive(false);
            currentSubMenu = null;
        }
    }

    /// <summary>
    /// Goes to the transcript menu.
    /// </summary>
    public void GoToTranscript()
    {
        SwitchToMenu(SubMenu.Transcript);
    }

    /// <summary>
    /// Goes to the save menu.
    /// </summary>
    public void GoToSave()
    {
        SwitchToMenu(SubMenu.Save);
    }

    /// <summary>
    /// Goes to the load menu.
    /// </summary>
    public void GoToLoad()
    {
        SwitchToMenu(SubMenu.Load);
    }

    /// <summary>
    /// Goes to the options menu.
    /// </summary>
    public void GoToOptions()
    {
        SwitchToMenu(SubMenu.Options);
    }

    /// <summary>
    /// Creates a confirmation window to go back to the main menu.
    /// </summary>
    public void ConfirmMainMenu()
    {
        string message = "Are you sure you want to return to the main menu?<br>This will lose unsaved progress.";
        GameManager.Instance.AddConfirmationWindow(FindFirstObjectByType<Canvas>().GetComponent<RectTransform>(), message, () => GameManager.Instance.LoadScene("Title"));
    }

    /// <summary>
    /// Goes to the compendium menu.
    /// </summary>
    public void GoToCompendium()
    {
        SwitchToMenu(SubMenu.Compendium);
    }

    /// <summary>
    /// Goes to the credits menu.
    /// </summary>
    public void GoToCredits()
    {
        SwitchToMenu(SubMenu.Credits);
    }

    /// <summary>
    /// Creates a confirmation window to exit the game.
    /// </summary>
    public void ConfirmExit()
    {
        //If this is the title scene, quit without any confirmation
        if (GameManager.Instance.GetSceneName() == "Title")
        {
            GameManager.Instance.QuitGame();
            return;
        }

        string message = "Are you sure you want to quit?";
        GameManager.Instance.AddConfirmationWindow(FindFirstObjectByType<Canvas>().GetComponent<RectTransform>(), message, () => GameManager.Instance.QuitGame());
    }

    /// <summary>
    /// Resumes the game.
    /// </summary>
    public void Resume()
    {
        if (GameManager.Instance.InGameMenu)
            ToggleGameMenu();
    }

    /// <summary>
    /// Switches to a new sub menu.
    /// </summary>
    /// <param name="newSubMenu">The new sub menu to show.</param>
    private void SwitchToMenu(SubMenu newSubMenu)
    {
        //If the game menu is animating, return
        if (animationActive)
            return;

        //Hide the current sub menu
        if (currentSubMenu != null)
            currentSubMenu.SetActive(false);

        //Set the new sub menu and show it
        currentSubMenu = subMenus[(int)newSubMenu];
        currentSubMenu.SetActive(true);
    }
}
