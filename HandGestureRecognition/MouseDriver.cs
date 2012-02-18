using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Drawing;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Windows.Forms;
using System.Windows;

namespace HandGestureRecognition
{
    class MouseDriver
    {
        int pointSetCount;
        ArrayList pointSets;
        Contour<PointF> movementContour;
        Vector[] vectors;

        public MouseDriver()
        {
            pointSetCount = 0;
            pointSets = new ArrayList();
            vectors = new Vector[3];
        }

        public MouseDriver(Seq<PointF> points, Contour<PointF> movementContour)
        {
            pointSetCount = 0;
            pointSets = new ArrayList();
            addFrame(points, movementContour);
        }

        public void addFrame(Seq<PointF> points, Contour<PointF> movementContour) 
        {
            if (pointSetCount < 5)
                pointSetCount++;
            else
                pointSets.RemoveAt(0);

            pointSets.Add(points);
        }

        public Vector[] getVectors()
        {
            Queue<System.Drawing.Point> tomake = new Queue<System.Drawing.Point>(5);
            foreach (Seq<PointF> pointSeq in pointSets) {
                int avX = 0;
                int avY = 0;
                foreach (PointF point in pointSeq)
                {
                    avX += (int)point.X;
                    avY += (int)point.Y;
                }
                avX /= 5;
                avY /= 5;
                tomake.Enqueue(new System.Drawing.Point(avX, avY));
            }
            foreach (System.Drawing.Point point in tomake)
            {
                Vector v = new Vector(tomake.Peek().X - 
            }
        }
    }
}
