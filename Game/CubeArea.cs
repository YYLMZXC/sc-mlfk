using System;
using System.Collections.Generic;
using Engine;

namespace Game
{
    public class CubeArea
    {
        public Point3 MaxPoint;

        public Point3 MinPoint;

        public Point3 Current;

        public Point3 Center;

        public int LengthX;

        public int LengthY;

        public int LengthZ;

        public bool IsPoint;

        public CubeArea(Point3 point1, Point3 point2)
        {
            MaxPoint = new Point3(MathUtils.Max(point1.X, point2.X), MathUtils.Max(point1.Y, point2.Y), MathUtils.Max(point1.Z, point2.Z));
            MinPoint = new Point3(MathUtils.Min(point1.X, point2.X), MathUtils.Min(point1.Y, point2.Y), MathUtils.Min(point1.Z, point2.Z));
            Current = MinPoint;
            LengthX = MathUtils.Abs(point1.X - point2.X) + 1;
            LengthY = MathUtils.Abs(point1.Y - point2.Y) + 1;
            LengthZ = MathUtils.Abs(point1.Z - point2.Z) + 1;
            Center = MinPoint + new Point3((int)((float)LengthX / 2f), (int)((float)LengthY / 2f), (int)((float)LengthZ / 2f));
            IsPoint = LengthX == 1 && LengthY == 1 && LengthZ == 1;
        }

        public bool Ergodic(Func<bool> action)
        {
            for (int i = 0; i < LengthX; i++)
            {
                for (int j = 0; j < LengthY; j++)
                {
                    for (int k = 0; k < LengthZ; k++)
                    {
                        Current = new Point3(i + MinPoint.X, j + MinPoint.Y, k + MinPoint.Z);
                        if (action())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void Ergodic(int division, float time, Action<Point3, Point3, Point3> action)
        {
            int num = LengthX / division + 1;
            int num2 = LengthY / division + 1;
            int num3 = LengthZ / division + 1;
            float num4 = 0f;
            List<Point3> points = new List<Point3>();
            int p = 0;
            for (int i = 0; i < num; i++)
            {
                for (int j = 0; j < num2; j++)
                {
                    for (int k = 0; k < num3; k++)
                    {
                        points.Add(new Point3(i, j, k));
                        Time.QueueTimeDelayedExecution(Time.RealTime + (double)num4, delegate
                        {
                            int x = MinPoint.X + points[p].X * division;
                            int y = MinPoint.Y + points[p].Y * division;
                            int z = MinPoint.Z + points[p].Z * division;
                            action(new Point3(x, y, z), points[p], points[points.Count - 1]);
                            p++;
                        });
                        num4 += time;
                    }
                }
            }
        }

        public void ErgodicByChunk(float perpareTime, float intervalTime, Action<Point3, Point2, Point2> action)
        {
            int num = LengthX / 16 + 1;
            int num2 = LengthZ / 16 + 1;
            action(MinPoint, new Point2(-1, -1), new Point2(-1, -1));
            float num3 = perpareTime;
            int p = 0;
            List<Point2> points = new List<Point2>();
            for (int i = 0; i < num; i++)
            {
                for (int j = 0; j < num2; j++)
                {
                    points.Add(new Point2(i, j));
                    Time.QueueTimeDelayedExecution(Time.RealTime + (double)num3, delegate
                    {
                        int x = MinPoint.X + points[p].X * 16;
                        int y = MinPoint.Y;
                        int z = MinPoint.Z + points[p].Y * 16;
                        action(new Point3(x, y, z), points[p], points[points.Count - 1]);
                        p++;
                    });
                    num3 += intervalTime;
                }
            }
        }

        public bool Exist(Vector3 target)
        {
            bool flag = target.X >= (float)MinPoint.X && target.Y >= (float)MinPoint.Y && target.Z >= (float)MinPoint.Z;
            bool flag2 = target.X <= (float)(MaxPoint.X + 1) && target.Y <= (float)(MaxPoint.Y + 1) && target.Z <= (float)(MaxPoint.Z + 1);
            return flag && flag2;
        }

        public bool Exist(Point3 target)
        {
            bool flag = target.X >= MinPoint.X && target.Y >= MinPoint.Y && target.Z >= MinPoint.Z;
            bool flag2 = target.X <= MaxPoint.X && target.Y <= MaxPoint.Y && target.Z <= MaxPoint.Z;
            return flag && flag2;
        }
    }
}