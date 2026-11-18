using System;
using black_hole_simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BlackHoleGraphic
{
    // Particle-only accretion disk rendered through Schwarzschild null geodesics.
    // One physical disk, multiple gravitationally lensed images; simplified optics.
    public class Game1 : Game
    {
        private const int WindowWidth = 1000;
        private const int WindowHeight = 700;
        private const int ImageWidth = 720;
        private const int ImageHeight = 470;

        // Elevation above the plane of the accretion disk.
        // Lower = flatter apparent disk.
        private const double ViewingElevation = 0.28; // radians, ~16 degrees
        private const double DiskInnerRadius = 3.05;   // Schwarzschild radii
        private const double DiskOuterRadius = 6.95;
        private const double MaxRayAngle = 5.0 * Math.PI;
        private const double RayStep = 0.035;
        private const double TimeScale = 0.65;

        // Particles live in the ONE physical disk, not on screen-space arcs.
        // Ray tracing projects this same density field into every lensed image.
        private const int ParticleCount = 8500;
        // Higher resolution keeps the 8500 emitters separate instead of blurring them.
        private const int DensityResolution = 640;
        // Radius of each luminous speck IN DISK SPACE, measured in Schwarzschild radii.
        // This changes dot size, not the original ray-traced silhouette.
        private const float ParticleDotRadius = 0.032f;
        private const float DensityScale = (DensityResolution - 1f) / (float)(2.0 * DiskOuterRadius);
        private readonly Random _random = new Random(173);
        private DiskParticle[] _particles;
        private float[] _particleDensity; // sparse, crisp particle stamp mask (not an accumulated glow)

        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _diskTexture;
        private Color[] _framePixels;
        private RayHit[] _rayMap;
        private BlackHole _blackHole;
        private double _schwarzschildRadius;
        private double _pixelsPerSchwarzschildRadius;
        private double _time;
        private double _renderAccumulator;

        private struct DiskParticle
        {
            public float Radius;
            public double Angle;
            public double AngularSpeed;
            public float Brightness;
        }

        private struct RayHit
        {
            public bool Visible;
            public float Radius;
            public float CosPhi;
            public float SinPhi;
            public float VelocityFactor;
            public float RedshiftFactor;
        }

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = WindowWidth;
            _graphics.PreferredBackBufferHeight = WindowHeight;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title = "ORIGINAL GEOMETRY - 8500 DISTINCT PARTICLES";
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _blackHole = new BlackHole(10.0 * PhysicsConstants.SolarMass);
            _schwarzschildRadius = _blackHole.CalculateSchwarzschildRadius();

            // Scale matches the previous view (around 45 pixels / Rs).
            _pixelsPerSchwarzschildRadius = _schwarzschildRadius / 650.0;
            _diskTexture = new Texture2D(GraphicsDevice, ImageWidth, ImageHeight, false, SurfaceFormat.Color);
            _framePixels = new Color[ImageWidth * ImageHeight];
            _rayMap = new RayHit[_framePixels.Length];

            InitializeParticles();
            PrecomputeRayMap();
            RebuildParticleDensity();
            RenderDisk();
        }

        private void PrecomputeRayMap()
        {
            double sinE = Math.Sin(ViewingElevation);
            double cosE = Math.Cos(ViewingElevation);

            for (int py = 0; py < ImageHeight; py++)
            {
                double screenY = ((ImageHeight - 1) * 0.5 - py) / _pixelsPerSchwarzschildRadius;

                for (int px = 0; px < ImageWidth; px++)
                {
                    double screenX = (px - (ImageWidth - 1) * 0.5) / _pixelsPerSchwarzschildRadius;
                    int index = py * ImageWidth + px;
                    double b = Math.Sqrt(screenX * screenX + screenY * screenY);
                    if (b < 0.0001) continue;

                    // The photon travels in the plane spanned by the observer's
                    // direction n and the normalized screen-offset direction p.
                    double pxUnit = screenX / b;
                    double pyUnit = screenY / b;
                    double pz = cosE * pyUnit;
                    double nz = sinE;

                    // Intersections of this photon plane with the z=0 disk plane.
                    // Multiple intersections are higher-order gravitational images
                    // of the SAME physical disk, not extra disks.
                    double nextDiskCrossing = Math.Atan2(nz, -pz);
                    if (nextDiskCrossing <= 0) nextDiskCrossing += Math.PI;

                    // Schwarzschild null geodesic: u'' + u = (3/2)u^2,
                    // u = Rs/r, with impact parameter b in units of Rs.
                    double u = 0.0;
                    double du = 1.0 / b;
                    double angle = 0.0;
                    bool found = false;

                    while (angle < MaxRayAngle)
                    {
                        double step = Math.Min(RayStep, nextDiskCrossing - angle);
                        if (step < 0.0000001)
                        {
                            if (u > 0.0 && u < 1.0)
                            {
                                double radius = 1.0 / u;
                                if (radius >= DiskInnerRadius && radius <= DiskOuterRadius)
                                {
                                    double ca = Math.Cos(angle);
                                    double sa = Math.Sin(angle);
                                    double diskX = (pxUnit * sa) / u;
                                    double diskY = (-cosE * ca + sinE * pyUnit * sa) / u;
                                    double diskAngle = Math.Atan2(diskY, diskX);

                                    // Local orbital velocity and approximate Doppler
                                    // factor; central gravitational redshift included.
                                    double beta = Math.Sqrt(1.0 / Math.Max(0.01, 2.0 * (radius - 1.0)));
                                    beta = Math.Min(beta, 0.85);
                                    double towardObserver = -Math.Cos(diskAngle) * cosE;
                                    double doppler = Math.Sqrt(1 - beta * beta) /
                                                     (1.0 - beta * towardObserver);
                                    double gravitational = Math.Sqrt(Math.Max(0.01, 1.0 - 1.0 / radius));

                                    _rayMap[index] = new RayHit
                                    {
                                        Visible = true,
                                        Radius = (float)radius,
                                        CosPhi = (float)Math.Cos(diskAngle),
                                        SinPhi = (float)Math.Sin(diskAngle),
                                        VelocityFactor = (float)doppler,
                                        RedshiftFactor = (float)gravitational
                                    };
                                    found = true;
                                    break;
                                }
                            }
                            nextDiskCrossing += Math.PI;
                            continue;
                        }

                        // Fourth-order Runge-Kutta integrates the photon orbit.
                        double k1u = du;
                        double k1v = -u + 1.5 * u * u;
                        double a2 = u + 0.5 * step * k1u;
                        double v2 = du + 0.5 * step * k1v;
                        double k2u = v2;
                        double k2v = -a2 + 1.5 * a2 * a2;
                        double a3 = u + 0.5 * step * k2u;
                        double v3 = du + 0.5 * step * k2v;
                        double k3u = v3;
                        double k3v = -a3 + 1.5 * a3 * a3;
                        double a4 = u + step * k3u;
                        double v4 = du + step * k3v;
                        double k4u = v4;
                        double k4v = -a4 + 1.5 * a4 * a4;
                        u += step * (k1u + 2 * k2u + 2 * k3u + k4u) / 6.0;
                        du += step * (k1v + 2 * k2v + 2 * k3v + k4v) / 6.0;
                        angle += step;

                        // Event horizon: the photon cannot escape to the observer.
                        if (u >= 1.0 || double.IsNaN(u) || double.IsInfinity(u)) break;
                        // Escaped to infinity on the opposite side, without a hit.
                        if (u < 0.0) break;
                    }

                    if (!found) _rayMap[index].Visible = false;
                }
            }
        }

        private void InitializeParticles()
        {
            _particles = new DiskParticle[ParticleCount];
            _particleDensity = new float[DensityResolution * DensityResolution];
            for (int i = 0; i < ParticleCount; i++)
            {
                // Area-aware initial distribution, gently biased inward.
                double u = _random.NextDouble();
                double r = Math.Sqrt(DiskInnerRadius * DiskInnerRadius +
                    u * (DiskOuterRadius * DiskOuterRadius - DiskInnerRadius * DiskInnerRadius));
                _particles[i] = new DiskParticle
                {
                    Radius = (float)r,
                    Angle = _random.NextDouble() * Math.PI * 2.0,
                    AngularSpeed = 7.0 / Math.Pow(r, 1.5),
                    Brightness = 0.65f + 0.35f * (float)_random.NextDouble()
                };
            }
        }

        // Build a sparse emission mask of small, discrete circular specks.
        // IMPORTANT: We do NOT modify PrecomputeRayMap(): the exact original
        // silhouette, dark shadow, upper arc and lower lensed image are retained.
        private void RebuildParticleDensity()
        {
            Array.Clear(_particleDensity, 0, _particleDensity.Length);
            float dotRadiusInGrid = ParticleDotRadius * DensityScale;
            float radiusSquared = dotRadiusInGrid * dotRadiusInGrid;

            for (int i = 0; i < _particles.Length; i++)
            {
                DiskParticle particle = _particles[i];
                float r = particle.Radius + 0.015f *
                    (float)Math.Sin(particle.Angle * 4.0 + _time * 1.7 + i * 0.17);
                float gx = (r * (float)Math.Cos(particle.Angle) +
                            (float)DiskOuterRadius) * DensityScale;
                float gy = (r * (float)Math.Sin(particle.Angle) +
                            (float)DiskOuterRadius) * DensityScale;

                int left = Math.Max(0, (int)Math.Floor(gx - dotRadiusInGrid));
                int right = Math.Min(DensityResolution - 1,
                    (int)Math.Ceiling(gx + dotRadiusInGrid));
                int top = Math.Max(0, (int)Math.Floor(gy - dotRadiusInGrid));
                int bottom = Math.Min(DensityResolution - 1,
                    (int)Math.Ceiling(gy + dotRadiusInGrid));

                for (int y = top; y <= bottom; y++)
                {
                    float dy = y - gy;
                    for (int x = left; x <= right; x++)
                    {
                        float dx = x - gx;
                        float distanceSquared = dx * dx + dy * dy;
                        if (distanceSquared > radiusSquared) continue;

                        // A hard, compact speck. No additive halos or bilinear blur.
                        float intensity = particle.Brightness *
                            (distanceSquared < radiusSquared * 0.28f ? 1f : 0.78f);
                        int index = y * DensityResolution + x;
                        if (intensity > _particleDensity[index])
                            _particleDensity[index] = intensity;
                    }
                }
            }
        }

        private float SampleParticleDensity(float x, float y)
        {
            int ix = (int)Math.Round((x + (float)DiskOuterRadius) * DensityScale);
            int iy = (int)Math.Round((y + (float)DiskOuterRadius) * DensityScale);
            if (ix < 0 || iy < 0 || ix >= DensityResolution || iy >= DensityResolution)
                return 0f;
            return _particleDensity[iy * DensityResolution + ix];
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
                return;
            }

            double dt = Math.Min(0.1, gameTime.ElapsedGameTime.TotalSeconds);
            _time += dt * TimeScale;
            _renderAccumulator += dt;
            // A CPU-computed animated emission texture at 30 Hz.
            if (_renderAccumulator >= 1.0 / 30.0)
            {
                double simulationDt = _renderAccumulator * TimeScale;
                _renderAccumulator = 0;
                for (int i = 0; i < _particles.Length; i++)
                {
                    DiskParticle particle = _particles[i];
                    particle.Angle += particle.AngularSpeed * simulationDt;
                    if (particle.Angle >= Math.PI * 2.0)
                        particle.Angle -= Math.PI * 2.0;
                    _particles[i] = particle;
                }
                RebuildParticleDensity();
                RenderDisk();
            }
            base.Update(gameTime);
        }

        private void RenderDisk()
        {
            // Ray-hit UV coordinates are unchanged from the ORIGINAL renderer.
            // Only the source of emitted light is now a sparse particle mask.
            for (int i = 0; i < _rayMap.Length; i++)
            {
                RayHit hit = _rayMap[i];
                if (!hit.Visible)
                {
                    _framePixels[i] = Color.Transparent;
                    continue;
                }

                float x = hit.Radius * hit.CosPhi;
                float y = hit.Radius * hit.SinPhi;
                float particle = SampleParticleDensity(x, y);
                if (particle <= 0f)
                {
                    _framePixels[i] = Color.Transparent;
                    continue;
                }

                double radial = Math.Max(0.0, Math.Min(1.0,
                    (hit.Radius - DiskInnerRadius) / (DiskOuterRadius - DiskInnerRadius)));
                double heat = Math.Pow(1.0 - radial, 1.3);
                double lensBrightness = Math.Min(1.6,
                    Math.Pow(Math.Min(1.8, hit.VelocityFactor * hit.RedshiftFactor), 2.0));
                double brightness = Math.Min(1.0,
                    particle * (0.52 + 0.48 * Math.Pow(1.0 - radial, 0.65)) *
                    lensBrightness * 1.7);

                byte red = (byte)(185 + 70 * heat);
                byte green = (byte)(62 + 174 * heat);
                byte blue = (byte)(10 + 118 * Math.Pow(heat, 2.5));
                // Opaque illuminated dots; everything else is transparent/black.
                _framePixels[i] = new Color(
                    (byte)(red * brightness),
                    (byte)(green * brightness),
                    (byte)(blue * brightness), (byte)255);
            }
            _diskTexture.SetData(_framePixels);
        }

        private static double SmoothStep(double edge0, double edge1, double x)
        {
            double t = Math.Max(0.0, Math.Min(1.0, (x - edge0) / (edge1 - edge0)));
            return t * t * (3.0 - 2.0 * t);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(2, 2, 6));
            Vector2 center = new Vector2(GraphicsDevice.Viewport.Width * 0.5f,
                                          GraphicsDevice.Viewport.Height * 0.5f);
            Vector2 topLeft = center - new Vector2(ImageWidth * 0.5f, ImageHeight * 0.5f);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            _spriteBatch.Draw(_diskTexture, topLeft, Color.White);
            _spriteBatch.End();
            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _diskTexture?.Dispose();
            base.UnloadContent();
        }
    }
}
