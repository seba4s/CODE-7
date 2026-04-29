using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Oculta el cursor del SO y dibuja una mira estilo sci-fi que sigue el mouse.
/// Se auto-instala en escenas de combate (tutorial + gameplay).
/// </summary>
public class CrosshairRuntime : MonoBehaviour
{
    // ── Auto-instalación ──────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInstall()
    {
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (!GameSceneConfig.IsCombatInputScene(scene.name)) return;
            if (FindAnyObjectByType<CrosshairRuntime>() != null) return;
            new GameObject("CrosshairRuntime").AddComponent<CrosshairRuntime>();
        };
    }

    // ── Estado ────────────────────────────────────────────────────
    RectTransform _crossRT;
    Canvas        _canvas;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
        BuildCrosshair();
    }

    void OnDestroy()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        if (_crossRT == null) return;
        _crossRT.position = GameInput.GetPointerPosition();
    }

    // ── Construcción ──────────────────────────────────────────────

    void BuildCrosshair()
    {
        // Canvas en overlay por encima de todo
        var cvGO = new GameObject("CrosshairCanvas");
        _canvas = cvGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;
        var scaler = cvGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ConstantPixelSize;
        cvGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Raíz de la mira (se mueve con el mouse)
        var crossGO = new GameObject("Crosshair");
        crossGO.transform.SetParent(cvGO.transform, false);
        _crossRT = crossGO.AddComponent<RectTransform>();
        _crossRT.sizeDelta = Vector2.zero;

        // Dibujamos la mira con 4 líneas + círculo central

        // Línea horizontal izquierda
        AddBar(crossGO.transform, "H_Left",  new Vector2(-20f, 0f), new Vector2(12f, 2f), false);
        // Línea horizontal derecha
        AddBar(crossGO.transform, "H_Right", new Vector2( 20f, 0f), new Vector2(12f, 2f), false);
        // Línea vertical arriba
        AddBar(crossGO.transform, "V_Up",    new Vector2(0f,  20f), new Vector2(2f, 12f), false);
        // Línea vertical abajo
        AddBar(crossGO.transform, "V_Down",  new Vector2(0f, -20f), new Vector2(2f, 12f), false);

        // Círculo central (pequeño punto)
        AddDot(crossGO.transform, "Dot", 4f);
    }

    void AddBar(Transform parent, string name, Vector2 offset, Vector2 size, bool horizontal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta        = size;
        rt.anchoredPosition = offset;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 1f, 0.5f, 0.92f);   // verde neón
    }

    void AddDot(Transform parent, string name, float radius)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(radius * 2f, radius * 2f);
        rt.anchoredPosition = Vector2.zero;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 1f, 0.5f, 1f);
    }
}
