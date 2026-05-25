using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// On editor startup: if no Match scene is open and Match.unity exists, open it.
/// Saves the developer one click every time they open the project.
/// </summary>
[InitializeOnLoad]
public static class AutoLoadMatchScene
{
    static AutoLoadMatchScene()
    {
        EditorApplication.delayCall += MaybeOpenMatch;
    }

    static void MaybeOpenMatch()
    {
        var active = EditorSceneManager.GetActiveScene();
        // If already on a real scene with content, don't disturb
        if (active.IsValid() && active.rootCount > 0) return;
        if (System.IO.File.Exists("Assets/Scenes/Match.unity"))
            EditorSceneManager.OpenScene("Assets/Scenes/Match.unity", OpenSceneMode.Single);
    }
}
