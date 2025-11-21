using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CompendiumViewer : MonoBehaviour
{
    [SerializeField, Tooltip("The RectTransform for the empty compendium.")] private RectTransform emptyTransform;
    [SerializeField, Tooltip("The RectTransform for the compendium viewer.")] private RectTransform compendiumViewerTransform;
    [SerializeField, Tooltip("The RectTransform for the basic info page.")] private RectTransform basicInfoTransform;
    [SerializeField, Tooltip("The RectTransform for the biography page.")] private RectTransform biographyTransform;
    [Space()]
    [SerializeField, Tooltip("The text to show the name.")] private TextMeshProUGUI nameText;
    [SerializeField, Tooltip("The text to show the power.")] private TextMeshProUGUI powerText;
    [SerializeField, Tooltip("The image for the profile.")] private Image profileImage;
    [Header("Basic Info")]
    [SerializeField, Tooltip("The text to show height.")] private TextMeshProUGUI heightText;
    [SerializeField, Tooltip("The text to show weight.")] private TextMeshProUGUI weightText;
    [SerializeField, Tooltip("The text to show birthdays.")] private TextMeshProUGUI birthdayText;
    [SerializeField, Tooltip("The text to show classifications.")] private TextMeshProUGUI classificationText;
    [SerializeField, Tooltip("The text to show subclassifications.")] private TextMeshProUGUI subclassificationText;
    [SerializeField, Tooltip("The text to show likes.")] private TextMeshProUGUI likesText;
    [SerializeField, Tooltip("The text to show dislikes.")] private TextMeshProUGUI dislikesText;
    [Space()]
    [Header("Biography Info")]
    [SerializeField, Tooltip("The text to show the biography.")] private TextMeshProUGUI biographyText;

    private PlayerControls playerControls;
    private CompendiumEntry currentEntry;
    private int currentIndex;

    private void Awake()
    {
        playerControls = new PlayerControls();
        playerControls.UI.LeftTab.performed += _ => ShowPreviousEntry();
        playerControls.UI.RightTab.performed += _ => ShowNextEntry();
    }

    private void OnEnable()
    {
        playerControls?.Enable();
        ShowCompendiumEntry(0);
    }

    private void OnDisable()
    {
        playerControls?.Disable();
    }

    private void ShowPreviousEntry() => FindNextEntry(-1);
    private void ShowNextEntry() => FindNextEntry(1);

    /// <summary>
    /// Finds the next unlocked entry in the list.
    /// </summary>
    /// <param name="increment">The incrementor for which direction to check in.</param>
    private void FindNextEntry(int increment)
    {
        //Go through the entries
        int startingIndex = currentIndex + increment;
        while(startingIndex >= 0 && startingIndex < GameManager.Instance.CompendiumEntryList.Length)
        {
            //If the next entry is unlocked, show it and exit the loop
            string entryName = GameManager.Instance.CompendiumEntryList[startingIndex].name;
            if (GameSettings.persistentData.compendiumEntriesUnlocked.Contains(entryName))
            {
                ShowCompendiumEntry(startingIndex);
                break;    
            }

            //Increment
            startingIndex += increment;
        }
    }

    /// <summary>
    /// Shows the character stored in the viewer.
    /// </summary>
    /// <param name="index">The index in the compendium entry to show.</param>
    public void ShowCompendiumEntry(int index)
    {
        //Set the compendium info
        currentIndex = index;
        currentEntry = GameManager.Instance.CompendiumEntryList[currentIndex];

        //Show the empty page if there is no information
        emptyTransform.gameObject.SetActive(currentEntry == null);
        compendiumViewerTransform.gameObject.SetActive(currentEntry != null);
        if (currentEntry == null)
            return;

        //Show the main character details
        nameText.text = currentEntry.name;
        powerText.text = currentEntry.powerTitle;
        profileImage.sprite = currentEntry.image;

        //Fill in the basic information
        heightText.text = currentEntry.height == 0 ? "???" : $"{currentEntry.height} cm ({DisplayFeetInches(currentEntry.height)})";
        weightText.text = currentEntry.weight == 0 ? "???" : $"{currentEntry.weight:0.#} kg ({DisplayPounds(currentEntry.weight)})";
        birthdayText.text = string.IsNullOrEmpty(currentEntry.classification) ? "???" : currentEntry.birthday;
        classificationText.text = string.IsNullOrEmpty(currentEntry.classification) ? "???" : currentEntry.classification;
        subclassificationText.text = string.IsNullOrEmpty(currentEntry.subClassifications) ? "???" : currentEntry.subClassifications;
        likesText.text = string.IsNullOrEmpty(currentEntry.likes) ? "???" : currentEntry.likes;
        dislikesText.text = string.IsNullOrEmpty(currentEntry.dislikes) ? "???" : currentEntry.dislikes;

        //Fill in the biography
        biographyText.text = string.IsNullOrEmpty(currentEntry.biography) ? "Information Not Available." : currentEntry.biography;
        //Change the alignment of the biography based on whether there is information available
        biographyText.alignment = string.IsNullOrEmpty(currentEntry.biography) ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft;

        //Show the basic info by default
        ShowBasicInfo();
    }

    /// <summary>
    /// Displays the basic information for the character.
    /// </summary>
    public void ShowBasicInfo()
    {
        //Show the page
        basicInfoTransform.gameObject.SetActive(true);
        biographyTransform.gameObject.SetActive(false);
    }

    /// <summary>
    /// Displays the biography for the character.
    /// </summary>
    public void ShowBiography()
    {
        //Show the page
        basicInfoTransform.gameObject.SetActive(false);
        biographyTransform.gameObject.SetActive(true);
    }

    /// <summary>
    /// Displays the height in feet / inches.
    /// </summary>
    /// <param name="cm">The height in centimeters.</param>
    /// <returns>A string with the feet / inches properly converted and formatted.</returns>
    private string DisplayFeetInches(double cm)
    {
        //Convert the cm to inches
        double totalInches = cm / 2.54;

        //Get the feet / inches
        int feet = (int)totalInches / 12;
        int inches = Mathf.RoundToInt((float)(totalInches % 12));

        //If there are 12 inches left over, convert it to a foot
        if(inches == 12)
        {
            feet++;
            inches = 0;
        }

        return feet.ToString() + "' " + inches.ToString() + "\"";
    }

    /// <summary>
    /// Displays the weight in pounds.
    /// </summary>
    /// <param name="kg">The weight in kilograms.</param>
    /// <returns>A string with the pounds properly converted and formatted.</returns>
    private string DisplayPounds(double kg) => Mathf.RoundToInt((float)(kg * 2.20462)).ToString("D0") + " lbs";
}
