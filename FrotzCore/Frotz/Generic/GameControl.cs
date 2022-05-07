using Frotz.Constants;

namespace Frotz.Generic
{

    public static class GameControl
    {
        public static void Undo()
        {
            if (Main.cwin == 0)
            {
            }
        }

        public static void SaveGame()
        {
        }

        public static void RestoreGame()
        {
            Process.zargc = 0;

            OS.ResetScreen();
        }
    }
}
