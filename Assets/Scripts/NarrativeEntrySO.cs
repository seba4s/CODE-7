using UnityEngine;

public enum NarrativeEntryType
{
    Photo,
    Chat,
    Letter
}

[CreateAssetMenu(menuName = "Narrative/Narrative Entry")]
public class NarrativeEntrySO : ScriptableObject
{
    public string id;                 // único, ej: "foto_001"
    public NarrativeEntryType type;
    public string title;
    [TextArea(5, 20)] public string body;
    public Sprite picture;            // opcional
}