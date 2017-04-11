using System.Windows.Forms;

namespace ScreenPixelRuler
{
    internal class Launcher
    {
        public static void Main()
        {
            Application.Run(new Ruler(600));
        }
    }
}
