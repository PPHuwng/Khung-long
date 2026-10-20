using UnityEditor;
using UnityEditor.SceneManagement;

namespace CityDino.Editor
{
    public static class RunnerSceneMenu
    {
        [MenuItem("City Dino/Open City Runner Scene", priority = 1)]
        public static void OpenRunnerScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/CityDino/Scenes/CityRunner.unity");
        }
    }
}
