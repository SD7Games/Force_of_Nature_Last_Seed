using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class RewardPopupPointerState
{
    public static bool IsAnyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Touchscreen touchscreen = Touchscreen.current;

        if (touchscreen != null)
        {
            var touches = touchscreen.touches;

            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i].press.isPressed)
                    return true;
            }
        }

        Mouse mouse = Mouse.current;

        if (mouse != null && mouse.leftButton.isPressed)
            return true;

        Pen pen = Pen.current;

        if (pen != null && pen.tip.isPressed)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0 || Input.GetMouseButton(0))
            return true;
#endif

        return false;
    }
}
