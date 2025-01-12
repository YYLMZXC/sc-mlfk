using System;
using Engine;

using Game;

namespace Mlfk
{
    public class ColumnArea
    {
        public int Radius;

        public int Height;

        public Point3 Center;

        public CoordDirection Coord;

        public Point3 MaxPoint;

        public Point3 MinPoint;

        public Point3 Current;

        public int LengthX;

        public int LengthY;

        public int LengthZ;

        public ColumnArea(int r, int h, Point3 c, CoordDirection coord)
        {
            Radius = r;
            Height = h;
            Center = c;
            Coord = coord;
            Current = Center;
            switch (Coord)
            {
                case CoordDirection.PX:
                    MinPoint = Center - new Point3(0, Radius, Radius);
                    MaxPoint = Center + new Point3(Height, Radius, Radius);
                    break;
                case CoordDirection.PY:
                    MinPoint = Center - new Point3(Radius, 0, Radius);
                    MaxPoint = Center + new Point3(Radius, Height, Radius);
                    break;
                case CoordDirection.PZ:
                    MinPoint = Center - new Point3(Radius, Radius, 0);
                    MaxPoint = Center + new Point3(Radius, Radius, Height);
                    break;
                case CoordDirection.NX:
                    MinPoint = Center - new Point3(Height, Radius, Radius);
                    MaxPoint = Center + new Point3(0, Radius, Radius);
                    break;
                case CoordDirection.NY:
                    MinPoint = Center - new Point3(Radius, Height, Radius);
                    MaxPoint = Center + new Point3(Radius, 0, Radius);
                    break;
                case CoordDirection.NZ:
                    MinPoint = Center - new Point3(Radius, Radius, Height);
                    MaxPoint = Center + new Point3(Radius, Radius, 0);
                    break;
            }

            LengthX = MaxPoint.X - MinPoint.X + 1;
            LengthY = MaxPoint.Y - MinPoint.Y + 1;
            LengthZ = MaxPoint.Z - MinPoint.Z + 1;
        }

        public void Ergodic(Action action)
        {
            for (int i = 0; i < LengthX; i++)
            {
                for (int j = 0; j < LengthY; j++)
                {
                    for (int k = 0; k < LengthZ; k++)
                    {
                        Current = new Point3(i + MinPoint.X, j + MinPoint.Y, k + MinPoint.Z);
                        int num = Current.X - Center.X;
                        int num2 = Current.Y - Center.Y;
                        int num3 = Current.Z - Center.Z;
                        int num4 = 0;
                        switch (Coord)
                        {
                            case CoordDirection.PX:
                                num4 = num2 * num2 + num3 * num3;
                                break;
                            case CoordDirection.NX:
                                num4 = num2 * num2 + num3 * num3;
                                break;
                            case CoordDirection.PY:
                                num4 = num * num + num3 * num3;
                                break;
                            case CoordDirection.NY:
                                num4 = num * num + num3 * num3;
                                break;
                            case CoordDirection.PZ:
                                num4 = num * num + num2 * num2;
                                break;
                            case CoordDirection.NZ:
                                num4 = num * num + num2 * num2;
                                break;
                        }

                        if (num4 <= Radius * Radius)
                        {
                            action();
                        }
                    }
                }
            }
        }

        public bool Exist(Point3 target)
        {
            bool flag = target.X >= MinPoint.X && target.Y >= MinPoint.Y && target.Z >= MinPoint.Z;
            bool flag2 = target.X <= MaxPoint.X && target.Y <= MaxPoint.Y && target.Z <= MaxPoint.Z;
            if (!(flag && flag2))
            {
                return false;
            }

            Point3 point = target - Center;
            int num = 0;
            switch (Coord)
            {
                case CoordDirection.PX:
                    num = point.Y * point.Y + point.Z * point.Z;
                    break;
                case CoordDirection.NX:
                    num = point.Y * point.Y + point.Z * point.Z;
                    break;
                case CoordDirection.PY:
                    num = point.X * point.X + point.Z * point.Z;
                    break;
                case CoordDirection.NY:
                    num = point.X * point.X + point.Z * point.Z;
                    break;
                case CoordDirection.PZ:
                    num = point.X * point.X + point.Y * point.Y;
                    break;
                case CoordDirection.NZ:
                    num = point.X * point.X + point.Y * point.Y;
                    break;
            }

            return num <= Radius * Radius;
        }
    }
}