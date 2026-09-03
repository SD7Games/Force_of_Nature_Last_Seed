using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zenject.Internal;

namespace LastSeed.Editor.Validation
{
    public static class ProjectDependencyValidationService
    {
        private const string ValidationMenuPath = "Tools/Last Seed/Validate Dependencies";

        [MenuItem(ValidationMenuPath)]
        public static void ValidateAllEnabledBuildScenes()
        {
            SceneSetup[] originalSceneSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                int validatedSceneCount = ZenUnityEditorUtil.ValidateAllActiveScenes();
                Debug.Log($"Last Seed dependency validation succeeded for {validatedSceneCount} enabled build scenes.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
            finally
            {
                if (ContainsLoadedScene(originalSceneSetup))
                    EditorSceneManager.RestoreSceneManagerSetup(originalSceneSetup);
            }
        }

        public static bool TryValidateCurrentScene()
        {
            bool validationCompleted = false;
            bool validationExecuted = ZenUnityEditorUtil.SaveThenRunPreserveSceneSetup(() =>
            {
                ZenUnityEditorUtil.ValidateCurrentSceneSetup();
                validationCompleted = true;
            });

            if (validationExecuted && validationCompleted)
                Debug.Log("Last Seed dependency validation succeeded for the current scene setup.");

            return validationExecuted && validationCompleted;
        }

        private static bool ContainsLoadedScene(SceneSetup[] sceneSetup)
        {
            for (int sceneIndex = 0; sceneIndex < sceneSetup.Length; sceneIndex++)
            {
                if (sceneSetup[sceneIndex].isLoaded)
                    return true;
            }

            return false;
        }
    }
}
