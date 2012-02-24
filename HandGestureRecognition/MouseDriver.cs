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
        Matrix<float> mouseState;
        Matrix<float> measurement;

        Queue<int[]> vectors;
        System.Drawing.Point last;
        private float CURR_SEN;
        private int VECT_COUNT;
        bool watching;
        Kalman kf;
        
        //Cursor Variables
        System.Drawing.Point position;

        public MouseDriver()
        {
            CURR_SEN = 20F;
            VECT_COUNT = 2;
            vectors = new Queue<int[]>(5);
            position = new System.Drawing.Point(0, 0);
            watching = false;

            float[,] tranisitionMatrix = {{1,0,1,0},{0,1,0,1},{0,0,1,0},{0,1,0,0}};
            float[,] predictedState = {{Cursor.Position.X,Cursor.Position.Y,0,0}};
            kf = new Kalman(4, 2, 0);
            Matrix<Int32> processNoise = new Matrix<Int32>(4, 1);
            measurement = new Matrix<float>(2, 1);
            measurement.SetValue(0);
            mouseState = new Matrix<float>(predictedState);

            kf.TransitionMatrix = new Matrix<float>(tranisitionMatrix);
            kf.PredictedState = new Matrix<float>(predictedState);
            kf.MeasurementMatrix.SetIdentity();
            
            kf.ProcessNoiseCovariance.SetIdentity(new MCvScalar(1e-4));
            kf.MeasurementNoiseCovariance.SetIdentity(new MCvScalar(1e-1));
            kf.ErrorCovariancePost.SetIdentity(new MCvScalar(.1));

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
                #region single point movement
                /*if (points != null)
                {
                    PointF[] test = points.ToArray();
                    if (test.Length > 0)
                    {
                        System.Drawing.Point posish = new System.Drawing.Point((int)test[0].X*3, (int)test[0].Y*3);
                        Cursor.Position = posish;
                    }
                }*/
                #endregion
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

                //work here
                System.Drawing.Point statePt = new System.Drawing.Point((int)mouseState[0,0],(int)mouseState[0,1]);
                Matrix<float> prediction = kf.Predict();
                System.Drawing.Point predictPt = new System.Drawing.Point((int)prediction[0,0],(int)prediction[0,1]);
                
                measurement[0,0] = Cursor.Position.X;
			    measurement[0,1] = Cursor.Position.Y;
			
			    victor.X = (int)measurement[0,0];
			    victor.Y = (int)measurement[0,1];
                Matrix<float> estimated = kf.Correct(measurement);

			    mrKalman.X = (int)estimated[0,0];
			    mrKalman.Y = (int)estimated[0,1];

                victor.X = newp.X - last.X;
                victor.Y = newp.Y - last.Y;
                //If not the first point
                last = newp;
            }
            return state;
        }

        private void UpdateCursor()
        {
            position = Cursor.Position;
            position.Offset((int)((mrKalman.X) * CURR_SEN), (int)((mrKalman.Y) * CURR_SEN * -1));
            Cursor.Position = position;
        }
    }
}
