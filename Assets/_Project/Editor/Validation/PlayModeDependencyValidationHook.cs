using UnityEditor;
using UnityEngine;

namespace LastSeed.Editor.Validation
{
    [InitializeOnLoad]
    public static class PlayModeDependencyValidationHook
    {
        private const string SkipNextValidationSessionKey =
            "LastSeed.DependencyValidation.SkipNextPlayModeValidation";

        private static bool _isPlayModeRestartScheduled;

        static PlayModeDependencyValidationHook()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange != PlayModeStateChange.ExitingEditMode)
                return;

            if (SessionState.GetBool(SkipNextValidationSessionKey, false))
            {
                SessionState.SetBool(SkipNextValidationSessionKey, false);
                return;
            }

            if (_isPlayModeRestartScheduled)
                return;

            _isPlayModeRestartScheduled = true;
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += ValidateAndRestartPlayMode;
        }

        private static void ValidateAndRestartPlayMode()
        {
            EditorApplication.delayCall -= ValidateAndRestartPlayMode;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += ValidateAndRestartPlayMode;
                return;
            }

            _isPlayModeRestartScheduled = false;

            if (!ProjectDependencyValidationService.TryValidateCurrentScene())
            {
                Debug.LogError("Play Mode was cancelled because the Zenject dependency graph is invalid.");
                return;
            }

            SessionState.SetBool(SkipNextValidationSessionKey, true);
            EditorApplication.isPlaying = true;
        }
    }
}
