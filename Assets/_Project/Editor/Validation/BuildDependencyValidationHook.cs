using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace LastSeed.Editor.Validation
{
    public sealed class BuildDependencyValidationHook : IPreprocessBuildWithReport
    {
        private const int ValidationCallbackOrder = -1000;

        public int callbackOrder => ValidationCallbackOrder;

        public void OnPreprocessBuild(BuildReport report)
        {
            ProjectDependencyValidationService.ValidateAllEnabledBuildScenes();
        }
    }
}
