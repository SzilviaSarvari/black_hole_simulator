using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace black_hole_simulator
{
    public class BlackHole
    {
        public double Mass { get; }

        public BlackHole(double mass)
        {
            if (mass <= 0 || !double.IsFinite(mass))
            {
                throw new ArgumentOutOfRangeException(nameof(mass), "Mass must be a positive finite number.");
            }

            Mass = mass;
        }   //A metódust osztályon belül, de konstruktoron kívül kell helyezni

            //Schwarzschild radius kalkuláció

            public double CalculateSchwarzschildRadius()
            {
                return (2 * PhysicsConstants.GravitationalConstant * Mass) / (PhysicsConstants.SpeedOfLight * PhysicsConstants.SpeedOfLight);
        }
        public double GravitationalAccelerationCalculation(double distance) { 

            if (distance <= 0 || !double.IsFinite(distance))
            {
                throw new ArgumentOutOfRangeException(nameof(distance), "Distance must be a positive finite number.");

            }
            return (PhysicsConstants.GravitationalConstant * Mass) / (distance * distance);
        }
        public double CalculateOrbitalVelocity(double distance)
        {
            if (distance <= 0 || !double.IsFinite(distance))
            {
                throw new ArgumentOutOfRangeException(nameof(distance), "Distance must be a positive finite number.");
            }
            return Math.Sqrt((PhysicsConstants.GravitationalConstant * Mass) / distance);
        }
        public double CalculatePhotonSphereRadius()
        {
            return 1.5 * CalculateSchwarzschildRadius();
        }

        public double CalculateInnermostStableOrbit()
        {
            return 3.0 * CalculateSchwarzschildRadius();
        }


        public double CalculateOrbitalPeriod(double distance)
        {
            double orbitalVelocity = CalculateOrbitalVelocity(distance);

            return 2 * Math.PI * distance / orbitalVelocity;
        }


        public double CalculateGravitationalTimeDilation(double distance)
        {
            if (!double.IsFinite(distance) ||
                distance <= CalculateSchwarzschildRadius())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(distance),
                    "Distance must be outside the event horizon.");
            }

            double schwarzschildRadius = CalculateSchwarzschildRadius();

            return Math.Sqrt(1 - schwarzschildRadius / distance);
        }


        public double CalculateGravitationalRedshift(double distance)
        {
            double factor = CalculateGravitationalTimeDilation(distance);

            return (1.0 / factor) - 1.0;
        }


    }
}
