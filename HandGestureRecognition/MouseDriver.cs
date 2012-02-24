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
using System.Runtime.InteropServices;

namespace HandGestureRecognition
{
    class MouseDriver
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(long dwFlags, long dx, long dy, long cButtons, long dwExtraInfo);
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        Seq<PointF> latestPoints;
        Seq<PointF> stickyPoints;
        Contour<System.Drawing.Point> movementContour;
        Vector victor, mrKalman;

        Queue<int[]> vectors;
        System.Drawing.Point last;
        private float CURR_SEN;
        bool watching;
        Kalman kf;
        SyntheticData kfData;
        
        //Cursor Variables
        System.Drawing.Point position;

        public MouseDriver()
        {
            CURR_SEN = 8.5F;
            vectors = new Queue<int[]>(5);
            position = new System.Drawing.Point(0, 0);
            watching = false;

            #region initialize Kalman filter
            kfData = new SyntheticData();

            kf = new Kalman(4, 2, 0);
            kf.TransitionMatrix = kfData.transitionMatrix;
            kf.MeasurementMatrix = kfData.measurementMatrix;
            kf.ProcessNoiseCovariance = kfData.processNoise;
            kf.MeasurementNoiseCovariance = kfData.measurementNoise;
            kf.ErrorCovariancePost = kfData.errorCovariancePost;
            kf.CorrectedState = kfData.state; // stan
            #endregion

            victor.X = 0;
            victor.Y = 0;
            mrKalman.X = 0;
            mrKalman.Y = 0;
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
                /*int state = 0;
                if (movementContour != null)
                {
                    MCvMoments mvMoments = movementContour.GetMoments();
                    MCvPoint2D64f mvCenter = mvMoments.GravityCenter;
                    state = UpdateVectors(fingerNum, mvCenter);
                }
                else
                    UpdateVectors(fingerNum, new MCvPoint2D64f());
                UpdateCursor();
                if (state == 1)
                {
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0);                    
                }*/

                UpdateVectors(fingerNum, new MCvPoint2D64f());
                UpdateCursor();
            }
            
            return watching;
        }

        /*private int CheckForMouseClicks(Contour<System.Drawing.Point> movementContour)
        {

            foreach (PointF point in latestPoints)
            {
                avX += (int)point.X / pointsCount;
                avY += (int)point.Y / pointsCount;
            }
            return 0;
        }*/

        private int UpdateVectors(int fingerNum, MCvPoint2D64f mvCenter)
        {
            int state = 0;
            if (latestPoints != null)
            {
                //Determine average X and Y of most recent point
                int avX = 0;
                int avY = 0;
                int pointsCount = latestPoints.Total;
                foreach (PointF point in latestPoints)
                {
                    avX += (int)point.X / pointsCount;
                    avY += (int)point.Y / pointsCount;
                    if (Math.Abs(mvCenter.x - point.X) + Math.Abs(mvCenter.x - point.X) < 10 && mvCenter.x != 0)
                        state = 1;
                }
                System.Drawing.Point newp = new System.Drawing.Point(avX, avY);

                Matrix<float> prediction = kf.Predict();
                PointF predictProint = new PointF(prediction[0, 0], prediction[1, 0]);
                // The mouse input points.
                PointF measurePoint = new PointF(kfData.GetMeasurement()[0, 0],
                    kfData.GetMeasurement()[1, 0]);
                Matrix<float> estimated = kf.Correct(kfData.GetMeasurement());
                // The resulting point from the Kalman Filter.
                System.Drawing.Point newpEst = new System.Drawing.Point((int)estimated[0, 0], (int)estimated[1, 0]);
                    //PointF estPoint = new PointF(estimated[0, 0], estimated[1, 0]);
                kfData.state[0, 0] = newp.X;
                kfData.state[1, 0] = newp.Y;
                
                mrKalman.X = newpEst.X - last.X;
                mrKalman.Y = newpEst.Y - last.Y;

                last = newpEst;
            }
            return state;
        }

        private void UpdateCursor()
        {
            position = Cursor.Position;
            position.Offset((int)((mrKalman.X) * CURR_SEN), (int)((mrKalman.Y) * CURR_SEN * -1));
            Cursor.Position = position;
            kfData.GoToNextState();
        }
    }
}
