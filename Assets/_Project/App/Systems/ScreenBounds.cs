using UnityEngine;

public static class ScreenBounds
{

    public static float Left;
    public static float Right;
    public static float Top;
    public static float Bottom;

    public static void Calculate(Camera cam)
    {
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

        Left = bottomLeft.x;
        Bottom = bottomLeft.y;
        Right = topRight.x;
        Top = topRight.y;
    }
}