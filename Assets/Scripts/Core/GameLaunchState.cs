namespace EmployeeHandbook.DailyTasks
{
    public enum GameStartMode
    {
        Auto,
        NewGame,
        ContinueGame
    }

    /// <summary>
    /// 跨 Scene 传递标题页选择的启动方式。
    /// </summary>
    public static class GameLaunchState
    {
        public static GameStartMode StartMode { get; private set; } = GameStartMode.Auto;

        public static bool HasRequestedMode
        {
            get { return StartMode != GameStartMode.Auto; }
        }

        public static void RequestNewGame()
        {
            StartMode = GameStartMode.NewGame;
        }

        public static void RequestContinueGame()
        {
            StartMode = GameStartMode.ContinueGame;
        }

        public static GameStartMode ConsumeStartMode()
        {
            GameStartMode mode = StartMode;
            StartMode = GameStartMode.Auto;
            return mode;
        }
    }
}
