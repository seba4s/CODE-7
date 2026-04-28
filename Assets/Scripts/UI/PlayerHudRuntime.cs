using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD runtime simple: barra de salud + barra de estamina.
/// Se auto-instala al cargar la escena.
/// </summary>
public class PlayerHudRuntime : MonoBehaviour
{
    PlayerHealth health;
    PlayerStamina stamina;

    Image hpFill;
    Image staminaFill;
    Text hpLabel;
    Text staminaLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInstall()
    {
        if (FindAnyObjectByType<PlayerHudRuntime>() != null) return;

        var go = new GameObject("PlayerHudRuntime");
        go.AddComponent<PlayerHudRuntime>();
    }

    void Start()
    {
        FindPlayerRefs();
        BuildHud();
        BindEvents();
        RefreshNow();
    }

    void OnDestroy()
    {
        UnbindEvents();
    }

    void FindPlayerRefs()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            var pc = FindAnyObjectByType<PlayerController2D>();
            if (pc != null) playerObj = pc.gameObject;
        }

        if (playerObj != null)
        {
            health = playerObj.GetComponent<PlayerHealth>();
            stamina = playerObj.GetComponent<PlayerStamina>();
        }
    }

    void BindEvents()
    {
        if (health != null)
            health.OnHealthChanged += HandleHealthChanged;

        if (stamina != null)
            stamina.OnStaminaChanged += HandleStaminaChanged;
    }

    void UnbindEvents()
    {
        if (health != null)
            health.OnHealthChanged -= HandleHealthChanged;

        if (stamina != null)
            stamina.OnStaminaChanged -= HandleStaminaChanged;
    }

    void RefreshNow()
    {
        if (health != null) HandleHealthChanged(health.currentHP, health.maxHP);
        if (stamina != null) HandleStaminaChanged(stamina.currentStamina, stamina.maxStamina);
    }

    void BuildHud()
    {
        if (GameObject.Find("RuntimeHUD") != null) return;

        var canvasGo = new GameObject("RuntimeHUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var root = CreateUIObject("Panel", canvasGo.transform);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(0f, 1f);
        rootRt.pivot = new Vector2(0f, 1f);
        rootRt.anchoredPosition = new Vector2(24f, -24f);
        rootRt.sizeDelta = new Vector2(330f, 120f);

        hpLabel = CreateLabel("HP", root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -4f), new Vector2(300f, 24f));
        hpFill = CreateBar(root.transform, new Vector2(0f, -30f), new Color(0.82f, 0.19f, 0.19f));

        staminaLabel = CreateLabel("STAMINA", root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -64f), new Vector2(300f, 24f));
        staminaFill = CreateBar(root.transform, new Vector2(0f, -90f), new Color(0.17f, 0.66f, 0.95f));
    }

    Image CreateBar(Transform parent, Vector2 anchoredPos, Color fillColor)
    {
        var barRoot = CreateUIObject("Bar", parent);
        var rt = barRoot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(300f, 22f);

        var bg = barRoot.AddComponent<Image>();
        bg.sprite = CreateWhiteSprite();
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        var fillGo = CreateUIObject("Fill", barRoot.transform);
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);

        var fill = fillGo.AddComponent<Image>();
        fill.sprite = CreateWhiteSprite();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.color = fillColor;
        fill.fillAmount = 1f;

        return fill;
    }

    Text CreateLabel(string text, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        var go = CreateUIObject(text + "Label", parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var t = go.AddComponent<Text>();
        t.font = GetUIFont();
        t.text = text;
        t.fontSize = 18;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleLeft;
        return t;
    }

    static Font GetUIFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    void HandleHealthChanged(int current, int max)
    {
        float p = max > 0 ? (float)current / max : 0f;
        if (hpFill != null) hpFill.fillAmount = Mathf.Clamp01(p);
        if (hpLabel != null) hpLabel.text = $"Salud: {current}/{max}";
    }

    void HandleStaminaChanged(float current, float max)
    {
        float p = max > 0f ? current / max : 0f;
        if (staminaFill != null) staminaFill.fillAmount = Mathf.Clamp01(p);
        if (staminaLabel != null) staminaLabel.text = $"Estamina: {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static Sprite CreateWhiteSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
