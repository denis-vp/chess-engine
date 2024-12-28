using Raylib_cs;

namespace chess_engine.UI
{
    public class BoardTheme
    {
        public Color LightCol = new Color(235, 236, 211, 255);
        public Color DarkCol = new Color(122, 148, 90, 255);

        public Color selectedLight = new Color(236, 197, 123, 255);
        public Color selectedDark = new Color(200, 158, 80, 255);

        public Color MoveFromLight = new Color(207, 172, 106, 255);
        public Color MoveFromDark = new Color(197, 158, 54, 255);

        public Color MoveToLight = new Color(221, 208, 124, 255);
        public Color MoveToDark = new Color(197, 173, 96, 255);

        public Color LegalLight = new Color(89, 171, 221, 255);
        public Color LegalDark = new Color(62, 144, 195, 255);

        public Color CheckLight = new Color(234, 74, 74, 255);
        public Color CheckDark = new Color(207, 39, 39, 255);

        public Color LightCoordCol = new Color(235, 236, 211, 255);
        public Color DarkCoordCol = new Color(122, 148, 90, 255);

        public Color TextCol = new Color(0, 0, 0, 255);
    }
}
