using black_hole_simulator;

//Creating new black hole --> new object --> call the constructor and passing the mass as a parameter

BlackHole blackhole = new BlackHole(PhysicsConstants.SolarMass);

//Calculating the radius --> method call

double radius = blackhole.CalculateSchwarzschildRadius();

//Printing the result

Console.WriteLine($"The Schwarzschild radius of a black hole with mass {blackhole.Mass} kg is {radius} meters.");


