using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Drawing;
using Emgu.CV.Structure;
using Emgu.CV;

namespace HandGestureRecognition
{
    class MouseDriver
    {
        int pointSetCount;
        ArrayList pointSets;
        Contour<PointF> movementContour;
        public MouseDriver()
        {
            pointSetCount = 0;
            pointSets = new ArrayList();
        }
        public MouseDriver(Seq<PointF> points, Contour<PointF> movementContour)
        {
            pointSetCount = 0;
            pointSets = new ArrayList();
            addFrame(points, movementContour);
        }

        public void addFrame(Seq<PointF> points, Contour<PointF> movementContour) 
        {
            if (pointSetCount < 15)
                pointSetCount++;
            else
                pointSets.RemoveAt(0);

            pointSets.Add(points);
        }
    }
}
