using System;

namespace TopSpeed.Bots
{
    public static partial class BotPhysics
    {
        private static float SmoothStep(float a, float b, float t)
        {
            var clamped = Clamp(t, 0f, 1f);
            clamped = clamped * clamped * (3f - 2f * clamped);
            return a + ((b - a) * clamped);
        }

        private static float Clamp(float v, float min, float max)
        {
            if (v < min)
                return min;
            if (v > max)
                return max;
            return v;
        }

        private static float DegToRad(float deg)
        {
            return (float)(Math.PI / 180.0) * deg;
        }
    }
}
