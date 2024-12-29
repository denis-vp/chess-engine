using Raylib_cs;
using System.Numerics;

namespace chess_engine
{
    static class Program
    {
        static Camera2D cam;

        public static void Main()
        {
            // Disable logging
            Raylib.SetTraceLogLevel(TraceLogLevel.None);

            Raylib.InitWindow(Settings.ScreenWidth, Settings.ScreenHeight, "Chess");

            Image icon = Raylib.LoadImage(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Resources.IconPath));
            Raylib.SetWindowIcon(icon);
            Raylib.UnloadImage(icon);

            Raylib.InitAudioDevice();
            Raylib.SetTargetFPS(60);

            UpdateCamera(Settings.ScreenWidth, Settings.ScreenHeight);

            GameController controller = new();

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(22, 22, 22, 255));
                Raylib.BeginMode2D(cam);

                controller.Update();
                controller.Draw();

                Raylib.EndMode2D();

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();

            controller.Release();
        }

        public static Vector2 ScreenToWorldPos(Vector2 screenPos) => Raylib.GetScreenToWorld2D(screenPos, cam);

        static void UpdateCamera(int screenWidth, int screenHeight)
        {
            cam = new Camera2D();
            cam.Offset = new Vector2(screenWidth / 2f, screenHeight / 2f);
            cam.Zoom = screenWidth / 1280f * 1.6f;
        }
    }
}
