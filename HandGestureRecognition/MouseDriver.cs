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
        Seq<PointF>[] pointSets;
        Contour<System.Drawing.Point> movementContour;
        Queue<Vector> vectors;
        System.Drawing.Point destination;

        public MouseDriver()
        {
            pointSetCount = 0;
            pointSets = new Seq<PointF>[5];
            vectors = new Queue<Vector>(4);
        }

        public MouseDriver(Seq<PointF> points, Contour<System.Drawing.Point> movementContour)
        {
            pointSetCount = 0;
            pointSets = new Seq<PointF>[5];
            AddFrame(points, movementContour);
        }

        public void AddFrame(Seq<PointF> points, Contour<System.Drawing.Point> movementContour) 
        {
            if (pointSetCount < 5)
                pointSetCount++;
            else
                pointSets.Dequeue();

            pointSets.Enqueue(points);
            UpdateVectors();
        }

        //change so it only changes 1 at a time
        public void UpdateVectors()
        {
            Queue<System.Drawing.Point> tomake = new Queue<System.Drawing.Point>(5);
            foreach (Seq<PointF> pointSeq in pointSets) {
                int avX = 0;
                int avY = 0;
                foreach (PointF point in pointSets)
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
                tomake.Dequeue();
                Vector v = new Vector(tomake.Peek().X - point.X, tomake.Peek().Y - point.Y);
                this.vectors.Enqueue(v);
            }
        }
    }
}
