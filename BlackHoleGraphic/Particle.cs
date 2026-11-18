using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using black_hole_simulator;
using Microsoft.Xna.Framework;

namespace BlackHoleGraphic
{
    public class Particle
    {
        public double OrbitRadius { get; set; }
        public double Angle { get; set; }
        public double AngularVelocity { get; set; }

        public Color ParticleColor { get; set; }
        public float Size { get; set; }
    }
}
