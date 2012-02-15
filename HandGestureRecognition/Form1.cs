﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emgu.CV.Structure;
using Emgu.CV;
using HandGestureRecognition.SkinDetector;
using Microsoft.Kinect;

namespace HandGestureRecognition
{
    public partial class Form1 : Form
    {

        IColorSkinDetector skinDetector;

        Image<Gray, Int16> currentFrame;
        Image<Gray, Int16> currentFrameCopy;

        int frameWidth;
        int frameHeight;
        private short[] pixelData;
        private short[] pixelDataLast;
        private byte[] depthFrame32;
        int last_top;

        Seq<Point> hull;
        Seq<Point> filteredHull;
        Seq<MCvConvexityDefect> defects;
        MCvConvexityDefect[] defectArray;
        Rectangle handRect;
        MCvBox2D box;
        Ellipse ellip;
        KinectSensor kinectSensor;

        Int32 MAX_INT32;
        Int32 MAX_INT16;
        public Form1()
        {
            InitializeComponent();
            box = new MCvBox2D();
            ellip = new Ellipse();
            last_top = 0;
            MAX_INT32 = Int32.MaxValue;
            MAX_INT16 = Int16.MaxValue;
            // show status for each sensor that is found now.
            foreach (KinectSensor kinect in KinectSensor.KinectSensors)
            {
                kinect.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                kinect.Start();
                kinect.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(DepthImageReady);
            }
        }

        void DepthImageReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame imageFrame = e.OpenDepthImageFrame())
            {
                if (imageFrame != null)
                {
                    if (pixelData == null)
                    {
                        frameWidth = imageFrame.Width;
                        frameHeight = imageFrame.Height;
                        pixelData = new short[imageFrame.PixelDataLength];
                        depthFrame32 = new byte[frameWidth * frameHeight * 4];
                        currentFrame = new Image<Gray, Int16>(frameWidth, frameHeight, new Gray(0.9));
                        pixelDataLast = new short[imageFrame.PixelDataLength];
                    }
                    imageFrame.CopyPixelDataTo(this.pixelData);
                    short[, ,] frameData = currentFrame.Data;
                    int pLength = imageFrame.PixelDataLength;
                    int closest = 32767;

                    for (int i = 0; i < pLength; i++)
                    {
                        int thisX = (int)(i % frameWidth);
                        int thisY = (int)(i / frameWidth);
                        short d = pixelData[i];
                        int temp = pixelData[i] - pixelDataLast[i];
                        /*if (d <= 0)
                            temp = MAX_INT16;
                        else
                            temp = pixelData[i] - pixelDataLast[i];
                         * */
                        if (d <= 0 || Math.Abs(temp) < 100 || d > 10000)
                            temp = 0;
                        else
                            temp = MAX_INT16;

                        frameData[thisY, thisX, 0] = (short)temp;

                        if (d < closest && d > 0)
                            closest = d;
                    }
                    last_top = closest;
                    pixelDataLast = (short[])pixelData.Clone();
                    dataOutput.Text = last_top.ToString();


                    //currentFrameCopy = currentFrame.Copy();

                    // Uncomment if using opencv adaptive skin detector
                    //Image<Gray,Byte> skin = new Image<Gray,byte>(currentFrameCopy.Width,currentFrameCopy.Height);                
                    //detector.Process(currentFrameCopy, skin);                

                    //skinDetector = new YCrCbSkinDetector();

                    //Image<Gray, Byte> skin = skinDetector.DetectSkin(currentFrameCopy, YCrCb_min, YCrCb_max);
                    Image<Gray, byte> temppp = currentFrame.Convert<byte>(delegate(short b) {return (byte) (b >> 8);});

                    ExtractContourAndHull(temppp);
                    DrawAndComputeFingersNum();
                    imageBoxFrameGrabber.Image = currentFrame;
                }
            }
        }
        private void ExtractContourAndHull(Image<Gray, byte> input)
        {
            using (MemStorage storage = new MemStorage())
            {
                Contour<Point> contours = input.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST, storage);
                Contour<Point> biggestContour = null;

                Double Result1 = 0;
                Double Result2 = 0;
                while (contours != null)
                {
                    Result1 = contours.Area;
                    if (Result1 > Result2)
                    {
                        Result2 = Result1;
                        biggestContour = contours;
                    }
                    contours = contours.HNext;
                }

                if (biggestContour != null)
                {
                    //currentFrame.Draw(biggestContour, new Bgr(Color.DarkViolet), 2);
                    Contour<Point> currentContour = biggestContour.ApproxPoly(biggestContour.Perimeter * 0.0025, storage);
                    currentFrame.Draw(currentContour, new Gray(MAX_INT32), 2);
                    biggestContour = currentContour;


                    hull = biggestContour.GetConvexHull(Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
                    box = biggestContour.GetMinAreaRect();
                    PointF[] points = box.GetVertices();
                    //handRect = box.MinAreaRect();
                    //currentFrame.Draw(handRect, new Bgr(200, 0, 0), 1);

                    Point[] ps = new Point[points.Length];
                    for (int i = 0; i < points.Length; i++)
                        ps[i] = new Point((int)points[i].X, (int)points[i].Y);

                    currentFrame.DrawPolyline(hull.ToArray(), true, new Gray(MAX_INT32), 2);
                    currentFrame.Draw(new CircleF(new PointF(box.center.X, box.center.Y), 3), new Gray(MAX_INT32), 2);

                    //ellip.MCvBox2D= CvInvoke.cvFitEllipse2(biggestContour.Ptr);
                    //currentFrame.Draw(new Ellipse(ellip.MCvBox2D), new Bgr(Color.LavenderBlush), 3);

                    //CvInvoke.cvMinEnclosingCircle(biggestContour.Ptr, out  center, out  radius);
                    //currentFrame.Draw(new CircleF(center, radius), new Bgr(Color.Gold), 2);

                    //currentFrame.Draw(new CircleF(new PointF(ellip.MCvBox2D.center.X, ellip.MCvBox2D.center.Y), 3), new Bgr(100, 25, 55), 2);
                    //currentFrame.Draw(ellip, new Bgr(Color.DeepPink), 2);

                    //CvInvoke.cvEllipse(currentFrame, new Point((int)ellip.MCvBox2D.center.X, (int)ellip.MCvBox2D.center.Y), new System.Drawing.Size((int)ellip.MCvBox2D.size.Width, (int)ellip.MCvBox2D.size.Height), ellip.MCvBox2D.angle, 0, 360, new MCvScalar(120, 233, 88), 1, Emgu.CV.CvEnum.LINE_TYPE.EIGHT_CONNECTED, 0);
                    //currentFrame.Draw(new Ellipse(new PointF(box.center.X, box.center.Y), new SizeF(box.size.Height, box.size.Width), box.angle), new Bgr(0, 0, 0), 2);


                    filteredHull = new Seq<Point>(storage);
                    for (int i = 0; i < hull.Total; i++)
                    {
                        if (Math.Sqrt(Math.Pow(hull[i].X - hull[i + 1].X, 2) + Math.Pow(hull[i].Y - hull[i + 1].Y, 2)) > box.size.Width / 10)
                        {
                            filteredHull.Push(hull[i]);
                        }
                    }

                    defects = biggestContour.GetConvexityDefacts(storage, Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);

                    defectArray = defects.ToArray();
                }
            }
        }

        private void DrawAndComputeFingersNum()
        {
            int fingerNum = 0;

            #region hull drawing
            //for (int i = 0; i < filteredHull.Total; i++)
            //{
            //    PointF hullPoint = new PointF((float)filteredHull[i].X,
            //                                  (float)filteredHull[i].Y);
            //    CircleF hullCircle = new CircleF(hullPoint, 4);
            //    currentFrame.Draw(hullCircle, new Bgr(Color.Aquamarine), 2);
            //}
            #endregion

            #region defects drawing
            if (defects != null)
            {
                for (int i = 0; i < defects.Total; i++)
                {
                    PointF startPoint = new PointF((float)defectArray[i].StartPoint.X,
                                                    (float)defectArray[i].StartPoint.Y);

                    PointF depthPoint = new PointF((float)defectArray[i].DepthPoint.X,
                                                    (float)defectArray[i].DepthPoint.Y);

                    PointF endPoint = new PointF((float)defectArray[i].EndPoint.X,
                                                    (float)defectArray[i].EndPoint.Y);

                    LineSegment2D startDepthLine = new LineSegment2D(defectArray[i].StartPoint, defectArray[i].DepthPoint);

                    LineSegment2D depthEndLine = new LineSegment2D(defectArray[i].DepthPoint, defectArray[i].EndPoint);

                    CircleF startCircle = new CircleF(startPoint, 5f);

                    CircleF depthCircle = new CircleF(depthPoint, 5f);

                    CircleF endCircle = new CircleF(endPoint, 5f);

                    //Custom heuristic based on some experiment, double check it before use
                    if ((startCircle.Center.Y < box.center.Y || depthCircle.Center.Y < box.center.Y) && (startCircle.Center.Y < depthCircle.Center.Y) && (Math.Sqrt(Math.Pow(startCircle.Center.X - depthCircle.Center.X, 2) + Math.Pow(startCircle.Center.Y - depthCircle.Center.Y, 2)) > box.size.Height / 6.5))
                    {
                        fingerNum++;
                        currentFrame.Draw(startDepthLine, new Gray(MAX_INT32), 2);
                        //currentFrame.Draw(depthEndLine, new Bgr(Color.Magenta), 2);
                    }


                    currentFrame.Draw(startCircle, new Gray(MAX_INT32), 2);
                    currentFrame.Draw(depthCircle, new Gray(MAX_INT32), 5);
                    //currentFrame.Draw(endCircle, new Bgr(Color.DarkBlue), 4);
                }
            }
            #endregion

            MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_DUPLEX, 5d, 5d);
            currentFrame.Draw(fingerNum.ToString(), ref font, new Point(50, 150), new Gray(MAX_INT32));
        }
    }
}