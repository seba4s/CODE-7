using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSceneConfig
{
    public const string MenuScene = "MainMenu";
    public const string ControlsScene = "ControlsScene";
    public const string TutorialScene = "TutorialScene";
    public const string GameplayScene = "SampleScene";

    static bool sceneHookInstalled;

    public static bool IsMenuScene(string sceneName) =>
        sceneName == MenuScene || sceneName == "MainMenu";

    public static bool IsControlsScene(string sceneName) =>
        sceneName == ControlsScene;

    public static bool IsTutorialScene(string sceneName) =>
        sceneName == TutorialScene;

    public static bool IsGameplayScene(string sceneName) =>
        sceneName == GameplayScene;

    public static bool IsCombatInputScene(string sceneName) =>
        IsTutorialScene(sceneName) || IsGameplayScene(sceneName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ForceMenuEntryPoint()
    {
        if (!sceneHookInstalled)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneHookInstalled = true;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        if (IsMenuScene(currentScene)) return;
        if (!Application.CanStreamedLevelBeLoaded(MenuScene)) return;

        SceneManager.LoadScene(MenuScene);
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsMenuScene(scene.name)) return;
        if (Object.FindAnyObjectByType<MainMenuRuntime>() != null) return;

        var menuRuntime = new GameObject("MainMenuRuntime");
        menuRuntime.AddComponent<MainMenuRuntime>();
    }

    public static bool CanLoadTutorialScene() =>
        Application.CanStreamedLevelBeLoaded(TutorialScene);

    public static string CurrentSceneName() =>
        SceneManager.GetActiveScene().name;
}
