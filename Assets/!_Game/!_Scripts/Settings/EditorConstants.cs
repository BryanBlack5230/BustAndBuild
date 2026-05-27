namespace Game.Configs
{
    public static class EditorConstants
    {
        public static class SceneWorkflow
        {
            private const string Prefix = "SceneWorkflow";

            public const string SavedScenesKey = Prefix + ".SavedScenes";

            public const string RunConfigPathKey = Prefix + ".RunConfigPath";
            public const string IsSingleSceneKey = Prefix + ".IsSingleScene";
            public const string SingleSceneTargetKey = Prefix + ".SingleSceneTarget";

            public static class Toolbox
            {
                private const string ToolboxPrefix = Prefix + ".Toolbox";

                public const string ConfigPath = ToolboxPrefix + ".ConfigPath";
                public const string SingleScene = ToolboxPrefix + ".SingleScene";
                public const string ShowPreview = ToolboxPrefix + ".ShowPreview";
            }
        }
    }
}