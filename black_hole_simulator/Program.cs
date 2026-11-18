using black_hole_simulator;

BlackHole blackhole = new BlackHole(PhysicsConstants.SolarMass);

double radius = blackhole.CalculateSchwarzschildRadius();

Console.WriteLine($"The Schwarzschild radius of a black hole with mass {blackhole.Mass} kg is {radius} meters.");


