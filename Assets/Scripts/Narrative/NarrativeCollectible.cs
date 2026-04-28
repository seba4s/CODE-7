using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NarrativeCollectible : MonoBehaviour
{
    [Header("Narrativa")]
    public NarrativeEntrySO entry;
    public NarrativeUI narrativeUI;

    [Header("Comportamiento")]
    public bool oneTimeOnly = true;
    public bool destroyOnCollect = true;

    bool consumed;

    void Awake()
    {
        if (narrativeUI == null)
            narrativeUI = FindAnyObjectByType<NarrativeUI>();
    }

    void Start()
    {
        if (entry == null) return;

        if (oneTimeOnly && NarrativeProgress.IsCollected(entry.id))
        {
            gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;

        bool isPlayer = other.CompareTag("Player")
            || other.GetComponent<PlayerController2D>() != null
            || other.GetComponentInParent<PlayerController2D>() != null;

        if (!isPlayer) return;
        Collect();
    }

    public void Collect()
    {
        if (consumed) return;
        consumed = true;

        if (entry == null)
        {
            Debug.LogWarning($"NarrativeCollectible en {name}: falta entry asignado.");
            return;
        }

        if (oneTimeOnly && !string.IsNullOrWhiteSpace(entry.id))
            NarrativeProgress.MarkCollected(entry.id);

        if (narrativeUI == null)
            narrativeUI = FindAnyObjectByType<NarrativeUI>();

        if (narrativeUI != null)
            narrativeUI.Show(entry);
        else
            Debug.LogWarning("NarrativeCollectible: no se encontro NarrativeUI en escena.");

        if (destroyOnCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
