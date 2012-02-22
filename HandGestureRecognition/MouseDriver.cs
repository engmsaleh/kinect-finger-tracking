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
        Seq<PointF> latestPoints;
        Seq<PointF> stickyPoints;
        Contour<System.Drawing.Point> movementContour;
        Queue<int[]> vectors;
        System.Drawing.Point last;
        private int CURR_SEN;
        bool watching;
        
        //Cursor Variables
        System.Drawing.Point position;

        public MouseDriver()
        {
            CURR_SEN = 1;
            vectors = new Queue<int[]>(4);
            position = new System.Drawing.Point(0, 0);
            watching = false;
        }

        public MouseDriver(Seq<PointF> points, int fingerNum, Contour<System.Drawing.Point> movementContour) : this()
        {
            AddFrame(points, fingerNum, movementContour);
        }

        public bool AddFrame(Seq<PointF> points, int fingerNum, Contour<System.Drawing.Point> movementContour) 
        {
            latestPoints = points;

            if (fingerNum > 4)
            {
                watching = true;
                stickyPoints = latestPoints;
            }
            else if (fingerNum < 1)
            {
                watching = false;
            }
            if (watching)
            {
                
                #region single point movement
                if (points != null)
                {
                    PointF[] test = points.ToArray();
                    if (test.Length > 0)
                    {
                        System.Drawing.Point posish = new System.Drawing.Point((int)test[0].X*3, (int)test[0].Y*3);
                        Cursor.Position = posish;
                    }
                }
                #endregion
                
                /*UpdateVectors(fingerNum);
                UpdateCursor();*/
            }
            return watching;
        }

        //change so it only changes 1 at a time
        private void UpdateVectors(int fingerNum)
        {
            if (latestPoints != null)
            {
                //Determine average X and Y of most recent point
                int avX = 0;
                int avY = 0;
                foreach (PointF point in latestPoints)
                {
                    avX += (int)point.X / fingerNum;
                    avY += (int)point.Y / fingerNum;
                }
                System.Drawing.Point newp = new System.Drawing.Point(avX, avY);

                //If not the first point
                if (last != null)
                    vectors.Enqueue(new int[] {newp.X - last.X, newp.Y - last.Y });
                last = newp;
                if (vectors.Count > 1)
                    vectors.Dequeue();
            }
        }

        private void UpdateCursor()
        {
            if (vectors.Count >= 5)
            {
                var itr = vectors.AsEnumerable();
                int[] addvectors = new int[2];
                position = Cursor.Position;

                foreach (int[] victor in itr)
                {
                    addvectors[0] += victor[0];
                    addvectors[1] += victor[1];
                }

                position.Offset((addvectors[0]) * CURR_SEN, (addvectors[1]) * CURR_SEN);
            }
            Cursor.Position = position;
        }
    }
}
