using BarkingBird.Runtime.Infrastructure.SceneWorkflow;

namespace BarkingBird.Editor
{
    public static class EditorConstants
    {
        // Delegate to the shared runtime constants so Editor tools
        // have a single, familiar access point.
        public static class SceneWorkflow
        {
            public static readonly string SavedScenesKey       = SceneWorkflowConstants.SavedScenesKey;
            public static readonly string RunConfigPathKey     = SceneWorkflowConstants.RunConfigPathKey;
            public static readonly string IsSingleSceneKey     = SceneWorkflowConstants.IsSingleSceneKey;
            public static readonly string SingleSceneTargetKey = SceneWorkflowConstants.SingleSceneTargetKey;

            public static class Toolbox
            {
                public static readonly string ConfigPath  = SceneWorkflowConstants.Toolbox.ConfigPath;
                public static readonly string SingleScene = SceneWorkflowConstants.Toolbox.SingleScene;
                public static readonly string ShowPreview = SceneWorkflowConstants.Toolbox.ShowPreview;
            }
        }
    }
}