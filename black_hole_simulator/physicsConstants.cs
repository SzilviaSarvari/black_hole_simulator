using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace black_hole_simulator
{
    public static class PhysicsConstants //azért static,mert nem akarjuk példányosítani, csak konstansokat tartalmaz
    {
        public const double GravitationalConstant = 6.67430e-11; // meg kell adni, hogy double, mert nem egész szám
        public const double SpeedOfLight = 299792458; // publicra kell tenni, hogy hozzáférhető legyen más osztályokból is és érdemes double --> túl nagy szám a végeredmény
        public const double SolarMass = 1.98847e30;
        //Pascal Case miatt kezdődik nagybetűvel, mert public, és a C# konvenciója szerint a public tagok nevei Pascal Case-ben vannak
    }
}
