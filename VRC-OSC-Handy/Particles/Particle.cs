using System.Collections.Generic;
using System.Windows;
using System.Windows.Shapes;

namespace VRC_OSC_Handy.Particles
{
    class Particle
    {
        /// <summary>
        /// shape
        /// </summary>
        public Ellipse Shape;
        /// <summary>
        /// coordinates
        /// </summary>
        public Point Position;
        /// <summary>
        /// speed
        /// </summary>
        public Vector Velocity;
        /// <summary>
        /// A collection of particles and line segments
        /// </summary>
        public Dictionary<Particle, Line> ParticleLines { get; set; }
    }
}
