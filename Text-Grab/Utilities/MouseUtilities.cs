using System.Windows.Forms;

public static class MouseUtilities
{
    public static (int X, int Y) GetMousePosition()
    {
        // Get the mouse position in screen coordinates
        int x = Cursor.Position.X;
        int y = Cursor.Position.Y;
        return (x, y);
    }
}