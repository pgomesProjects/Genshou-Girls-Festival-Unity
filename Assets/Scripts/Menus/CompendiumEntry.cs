using UnityEngine;

[CreateAssetMenu(fileName = "New Compendium Entry", menuName = "Scriptable Objects/Compendium Entry")]
public class CompendiumEntry : ScriptableObject
{
    [Tooltip("The name of the person.")] public new string name;
    [Tooltip("The person's power title, if any.")] public string powerTitle;
    [Tooltip("The image of the person.")] public Sprite image;
    [Space()]
    [Header("Basic Info")]
    [SerializeField, Tooltip("The height of the person (in cm).")] public double height;
    [SerializeField, Tooltip("The weight of the person (in kg).")] public double weight;
    [SerializeField, Tooltip("The birthday of the person.")] public string birthday;
    [SerializeField, Tooltip("The classification of the person.")] public string classification;
    [SerializeField, Tooltip("The sub classification of the person (if any).")] public string subClassifications;
    [SerializeField, Tooltip("The likes of the person.")] public string likes;
    [SerializeField, Tooltip("The dislikes of the person.")] public string dislikes;
    [Space()]
    [Tooltip("The biography of the person."), TextArea] public string biography;
}
