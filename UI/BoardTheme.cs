using Raylib_cs;

namespace chess_engine.UI
{
    public class BoardTheme
    {
        public Color LightColor = new Color(235, 236, 211, 255);
        public Color DarkColor = new Color(122, 148, 90, 255);

        public Color MoveFromLight = new Color(245, 244, 153, 255);
        public Color MoveFromDark = new Color(189, 201, 93, 255);

        public Color MoveToLight = new Color(245, 244, 153, 255);
        public Color MoveToDark = new Color(189, 201, 93, 255);

        public Color LegalLight = new Color(0, 205, 205, 255);
        public Color LegalDark = new Color(0, 235, 235, 255);

        public Color CheckLight = new Color(255, 0, 0, 255);
        public Color CheckDark = new Color(255, 0, 0, 255);

        public Color LightCoordColor = new Color(235, 236, 211, 255);
        public Color DarkCoordColor = new Color(122, 148, 90, 255);
    }
}
