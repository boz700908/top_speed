using TopSpeed.Data;

namespace TopSpeed.Physics.Surface
{
    public static class SurfaceModel
    {
        public static SurfaceModifiers Resolve(TrackSurface surface, float baseTraction)
        {
            var traction = baseTraction;
            var brake = 1.0f;
            var rollingResistance = 1.0f;
            var lateralMultiplier = 1.0f;

            switch (surface)
            {
                case TrackSurface.Gravel:
                    traction = (traction * 2f) / 3f;
                    brake *= 2f / 3f;
                    rollingResistance = 1.20f;
                    break;
                case TrackSurface.Water:
                    traction = (traction * 3f) / 5f;
                    brake *= 3f / 5f;
                    rollingResistance = 1.08f;
                    break;
                case TrackSurface.Sand:
                    traction *= 0.5f;
                    brake *= 3f / 2f;
                    rollingResistance = 1.75f;
                    break;
                case TrackSurface.Snow:
                    brake *= 0.5f;
                    rollingResistance = 1.15f;
                    lateralMultiplier = 1.44f;
                    break;
            }

            return new SurfaceModifiers(traction, brake, rollingResistance, lateralMultiplier);
        }
    }
}
