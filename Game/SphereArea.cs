using System;
using Engine;

namespace Game
{
    public class SphereArea
    {
        public int Radius;

        public int SquaRadius;

        public Point3 Center;

        public Point3 Current;

        public Point3 MaxPoint;

        public Point3 MinPoint;

        public SphereArea(int r, Point3 c)
        {
            Radius = r;
            SquaRadius = r * r;
            Center = c;
            Current = Center;
            MaxPoint = Center + new Point3(Radius);
            MinPoint = Center - new Point3(Radius);
        }

        public void Ergodic(Action action)
        {
            for (int i = -Radius + 1; i < Radius; i++)
            {
                for (int j = -Radius + 1; j < Radius; j++)
                {
                    for (int k = -Radius + 1; k < Radius; k++)
                    {
                        if (i * i + j * j + k * k <= SquaRadius)
                        {
                            Current = new Point3(Center.X + i, Center.Y + j, Center.Z + k);
                            action();
                        }
                    }
                }
            }
        }

        public bool Exist(Point3 target)
        {
            Point3 point = target - Center;
            if (point.X * point.X == SquaRadius || point.Y * point.Y == SquaRadius || point.Z * point.Z == SquaRadius)
            {
                return false;
            }

            return point.X * point.X + point.Y * point.Y + point.Z * point.Z <= SquaRadius;
        }
    }
}