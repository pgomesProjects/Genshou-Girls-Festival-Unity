using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PersistentData
{
    [Tooltip("If true, this is the first time the user opened up the application.")] public bool firstApplicationRun;
    [Tooltip("The current save slot for the auto saves.")] public int currentAutoSaveSlot;
    [Tooltip("The current list of compendium entries locked.")] public List<string> compendiumEntriesUnlocked;

    public PersistentData()
    {
        firstApplicationRun = true;
        currentAutoSaveSlot = 0;
        compendiumEntriesUnlocked = new List<string>();

        //Unlock the player's entry by default
        compendiumEntriesUnlocked.Add("Holly Rivers");
    }

    /// <summary>
    /// Unlocks an entry in the compendium.
    /// </summary>
    /// <param name="entryName">The name in the compendium entry.</param>
    public void UnlockEntry(string entryName)
    {
        //If the compendium doesn't have the entry already unlocked, unlocked
        if (!compendiumEntriesUnlocked.Contains(entryName))
            compendiumEntriesUnlocked.Add(entryName);

        //Save the persistent data
        GameSettings.SavePersistentData();
    }
}
