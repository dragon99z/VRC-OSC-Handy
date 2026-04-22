using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VRC_OSC_Handy.Particles
{
    class ParticleSystem
    {

        /// <summary>
        /// Number of particles
        /// </summary>
        private int particleCount = 50;

        /// <summary>
        /// Minimum particle size
        /// </summary>
        private static int sizeMin = 1;

        /// <summary>
        /// Maximum particle size
        /// </summary>
        private int sizeMax = 5;

        /// <summary>
        /// Particle movement speed
        /// </summary>
        private int speed = 5;

        /// <summary>
        /// Threshold for marking
        /// </summary>
        private int lineThreshold = 25;

        /// <summary>
        /// Mouse radius
        /// </summary>
        private static int mouseRadius = 20;

        /// <summary>
        /// random number 
        /// </summary>
        private Random random;

        /// <summary>
        /// Particle list
        /// </summary>
        private List<Particle> particles;

        /// <summary>
        /// Particle container
        /// </summary>
        private Canvas containerParticles;

        /// <summary>
        /// Line segment container
        /// </summary>
        private Grid containerLine;


        public ParticleSystem(int _maxRadius, int _particleCount, int _speed, int _lineThreshold, int _mouseRadius, Canvas _containerParticles, Grid _containerLine)
        {
            particleCount = _particleCount;
            speed = _speed;
            sizeMax = _maxRadius;
            lineThreshold = _lineThreshold;
            mouseRadius = _mouseRadius;
            containerLine = _containerLine;
            containerParticles = _containerParticles;
            random = new Random();
            particles = new List<Particle>();
            SpawnParticle();
        }

        /// <summary>
        /// Initialize particles
        /// </summary>
        private void SpawnParticle()
        {
            //Empty the particle queue
            particles.Clear();
            containerLine.Children.Clear();
            containerParticles.Children.Clear();

            //Generate particles
            for (int i = 0; i < particleCount; i++)
            {
                double size = random.Next(sizeMin, sizeMax + 1);
                Particle p = new Particle
                {
                    Shape = new Ellipse
                    {
                        Width = size,
                        Height = size,
                        Stretch = Stretch.Fill,
                        Fill = new SolidColorBrush(Color.FromArgb(125, 255, 255, 255)),
                    },
                    Position = new Point(random.Next(0, (int)containerParticles.ActualWidth), random.Next(0, (int)containerParticles.ActualHeight)),
                    Velocity = new Vector(random.Next(-speed, speed) * 0.1, random.Next(-speed, speed) * 0.1),
                    ParticleLines = new Dictionary<Particle, Line>()
                };
                particles.Add(p);
                Canvas.SetLeft(p.Shape, p.Position.X);
                Canvas.SetTop(p.Shape, p.Position.Y);
                containerParticles.Children.Add(p.Shape);
            }
        }

        /// <summary>
        /// Particle roaming animation
        /// </summary>
        public void ParticleRoamUpdate(Point mp)
        {
            foreach (Particle p in particles)
            {
                p.Position.X = p.Position.X + p.Velocity.X;
                p.Position.Y = p.Position.Y + p.Velocity.Y;

                if (p.Position.X < p.Shape.Width)
                    p.Position.X = p.Shape.Width;
                if (p.Position.Y < p.Shape.Height)
                    p.Position.Y = p.Shape.Height;
                if (p.Position.X > containerParticles.ActualWidth - p.Shape.Width)
                    p.Position.X = containerParticles.ActualWidth - p.Shape.Width;
                if (p.Position.Y > containerParticles.ActualHeight - p.Shape.Height)
                    p.Position.Y = containerParticles.ActualHeight - p.Shape.Height;

                //The speed is 0 judgment
                if (p.Velocity.X == 0) p.Velocity.X = random.Next(-speed, speed) * 0.1;
                if (p.Velocity.Y == 0) p.Velocity.Y = random.Next(-speed, speed) * 0.1;

                //Whether it collides with the wall
                if ((p.Position.X <= p.Shape.Width) || (p.Position.X >= containerParticles.ActualWidth - p.Shape.Width))
                    p.Velocity.X = -p.Velocity.X;
                if ((p.Position.Y <= p.Shape.Height) || (p.Position.Y >= containerParticles.ActualHeight - p.Shape.Height))
                    p.Velocity.Y = -p.Velocity.Y;

                //Mouse movement changes particle position
                //Find the distance from the point to the center of the circle
                double c = Math.Pow(Math.Pow(mp.X - p.Position.X, 2) + Math.Pow(mp.Y - p.Position.Y, 2), 0.75);
                if (c < mouseRadius)
                {
                    p.Position.X = mp.X - ((mp.X - p.Position.X) * mouseRadius / c);
                    p.Position.Y = (p.Position.Y - mp.Y) * mouseRadius / c + mp.Y;
                }

                Canvas.SetLeft(p.Shape, p.Position.X);
                Canvas.SetTop(p.Shape, p.Position.Y);
            }
        }

        /// <summary>
        /// Add or remove lines between particles
        /// </summary>
        public void AddOrRemoveParticleLine()
        {
            for (int i = 0; i < particleCount - 1; i++)
            {
                for (int j = i + 1; j < particleCount; j++)
                {
                    Particle p1 = particles[i];
                    double x1 = p1.Position.X + p1.Shape.Width / 2;
                    double y1 = p1.Position.Y + p1.Shape.Height / 2;
                    Particle p2 = particles[j];
                    double x2 = p2.Position.X + p2.Shape.Width / 2;
                    double y2 = p2.Position.Y + p2.Shape.Height / 2;
                    double s = Math.Sqrt((y2 - y1) * (y2 - y1) + (x2 - x1) * (x2 - x1));//The distance between two particles
                    if (s <= lineThreshold)
                    {
                        if (!p1.ParticleLines.ContainsKey(p2))
                        {
                            Line line = new Line()
                            {
                                X1 = x1,
                                Y1 = y1,
                                X2 = x2,
                                Y2 = y2,
                                Stroke = new SolidColorBrush(Color.FromArgb(125, 255, 255, 255)),
                            };
                            p1.ParticleLines.Add(p2, line);
                            containerLine.Children.Add(line);
                        }
                    }
                    else
                    {
                        if (p1.ParticleLines.ContainsKey(p2))
                        {
                            containerLine.Children.Remove(p1.ParticleLines[p2]);
                            p1.ParticleLines.Remove(p2);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Move the connection between particles
        /// </summary>
        public void MoveParticleLine()
        {
            foreach (Particle p in particles)
            {
                foreach (var starLine in p.ParticleLines)
                {
                    Line line = starLine.Value;
                    line.X1 = p.Position.X + p.Shape.Width / 2;
                    line.Y1 = p.Position.Y + p.Shape.Height / 2;
                    line.X2 = starLine.Key.Position.X + starLine.Key.Shape.Width / 2;
                    line.Y2 = starLine.Key.Position.Y + starLine.Key.Shape.Height / 2;
                }
            }


        }
    }
}
