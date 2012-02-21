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
        Seq<PointF> latestPoints;
        Contour<System.Drawing.Point> movementContour;
        //Queue<Vector> vectors;
        Queue<int[]> vectors;
        System.Drawing.Point last;
        
        //Cursor Variables
        System.Drawing.Point position;
        System.Drawing.Point destination;

        public MouseDriver()
        {
            pointSetCount = 0;
            vectors = new Queue<int[]>(4);
            position = new System.Drawing.Point(0, 0);
            destination = new System.Drawing.Point(0, 0);
        }

        public MouseDriver(Seq<PointF> points, Contour<System.Drawing.Point> movementContour)
        {
            pointSetCount = 0;
            AddFrame(points, movementContour);
            position = new System.Drawing.Point(0, 0);
            destination = new System.Drawing.Point(0, 0);
        }

        public void AddFrame(Seq<PointF> points, Contour<System.Drawing.Point> movementContour) 
        {
            latestPoints = points;
            UpdateVectors();
            UpdateCursor();
        }

        //change so it only changes 1 at a time
        public void UpdateVectors()
        {
            if (latestPoints != null)
            {
                //Determine average X and Y of most recent point
                int avX = 0;
                int avY = 0;
                foreach (PointF point in latestPoints)
                {
                    avX += (int)point.X;
                    avY += (int)point.Y;
                }
                avX /= 5;
                avY /= 5;
                System.Drawing.Point temp = new System.Drawing.Point(avX, avY);

                //If first point
                if (last == null)
                {
                    last = temp;
                    position = temp;
                }
                //If there is a last point
                else
                {
                    vectors.Enqueue(new int[] {temp.X - last.X, temp.Y - last.Y});
                    last = temp;
                }
            }
        }

        public void UpdateCursor()
        {
            if (vectors.Count == 4)
            {
                var itr = vectors.AsEnumerable();
                int[] addvectors = new int[2];
                foreach (int[] victor in itr)
                {
                    addvectors[0] += victor[0];
                    addvectors[1] += victor[1];
                }
                destination = position;
                destination.Offset(addvectors[0], addvectors[1]);

                position.Offset(addvectors[0] / 5, addvectors[1] / 5);


            }
            Cursor.Position = position;
        }
    }
}
