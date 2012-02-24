using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emgu.CV.Structure;
using Emgu.CV;
using Microsoft.Kinect;
using System.Collections;

namespace HandGestureRecognition
{
    public partial class Form1 : Form
    {

        Image<Gray, Int16> currentFrame;
        Image<Gray, byte> movement;
        Image<Bgr, byte> colorFrame;
        MouseDriver mouse;
        Contour<Point> shapeContour;

        int frameWidth;
        int frameHeight;
        private short[] tableData;
        private short[] pixelData;
        private short[] pixelDataLast;
        private byte[] depthFrame32;
        bool recalibrate;

        Kalman kalman;

        Seq<Point> hull;
        Seq<Point> filteredHull;
        Seq<MCvConvexityDefect> defects;
        MCvConvexityDefect[] defectArray;
        MCvBox2D box;

        Int32 MAX_INT32;
        Int32 MAX_INT16;


        //eddie
        Seq<PointF> dPointList; // All the points on the contour
        Seq<PointF> realendPointList;

        public Form1()
        {
            InitializeComponent();
            box = new MCvBox2D();
            mouse = new MouseDriver();
            MAX_INT32 = Int32.MaxValue;
            MAX_INT16 = Int16.MaxValue;
            recalibrate = true;
            // show status for each sensor that is found now.
            foreach (KinectSensor kinect in KinectSensor.KinectSensors)
            {
                kinect.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                kinect.Start();
                kinect.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(DepthImageReady);
            }
        }

        private void calibrate(DepthImageFrame cal)
        {
            tableData = new short[cal.PixelDataLength];
            cal.CopyPixelDataTo(this.tableData);
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
                        currentFrame = new Image<Gray, Int16>(frameWidth, frameHeight, new Gray(0));
                        movement = new Image<Gray, byte>(frameWidth, frameHeight, new Gray(0));
                        pixelDataLast = new short[imageFrame.PixelDataLength];
                    }
                    if (recalibrate)
                    {
                        calibrate(imageFrame);
                        recalibrate = false;
                    }
                    imageFrame.CopyPixelDataTo(this.pixelData);
                    short[, ,] frameData = currentFrame.Data;
                    byte[, ,] moveData = movement.Data;
                    int pLength = imageFrame.PixelDataLength;

                    for (int i = 0; i < pLength; i++)
                    {
                        int thisX = (int)(i % frameWidth);
                        int thisY = (int)(i / frameWidth);
                        short d = pixelData[i];
                        int moveCalc = pixelData[i] - pixelDataLast[i];
                        int temp;
                        int tableTemp = pixelData[i] - tableData[i];
                        /*if (d <= 0)
                            temp = MAX_INT16;
                        else
                            temp = pixelData[i] - pixelDataLast[i];
                        * */
                        if (d <= 0 || Math.Abs(tableTemp) < 50 || d > 10000)
                            temp = 0;
                        else
                            temp = MAX_INT16;
                        if (d >= 0 && Math.Abs(moveCalc) > 75 && d < 10000)
                            moveData[thisY, thisX, 0] = 255;
                        else
                            moveData[thisY, thisX, 0] = 0;
                        frameData[thisY, thisX, 0] = (short)temp;
                    }
                    pixelDataLast = (short[])pixelData.Clone();

                    Image<Gray, byte> cFrameByte = currentFrame.Convert<byte>(delegate(short b) { return (byte)(b >> 8); });
                    colorFrame = cFrameByte.Convert<Bgr, byte>();

                    if(ExtractContourAndHull(cFrameByte))
                        DrawAndComputeFingersNum();

                    imageBoxFrameGrabber.Image = colorFrame;
                }
            }
        }
        private bool ExtractContourAndHull(Image<Gray, byte> input)
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
                    if (Result1 > Result2 && Result1 > 300)
                    {
                        Result2 = Result1;
                        biggestContour = contours;
                    }
                    contours = contours.HNext;
                }

                Contour<Point> mvContours = movement.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST, storage);
                Contour<Point> biggestMovement = null;
                Result2 = 0;
                while (mvContours != null)
                {
                    Result1 = mvContours.Area;
                    if (Result1 > 150 && Result1 > Result2)
                    {
                        Result2 = Result1;
                        biggestMovement = mvContours.ApproxPoly(mvContours.Perimeter * 0.0025, storage);
                        colorFrame.Draw(biggestMovement, new Bgr(0, 0, 255), 2);
                    }
                    mvContours = mvContours.HNext;
                }
                mvContours = null;
                shapeContour = biggestMovement;

                if (biggestContour != null)
                {
                    Contour<Point> currentContour = biggestContour.ApproxPoly(biggestContour.Perimeter * 0.0025, storage);
                    currentFrame.Draw(currentContour, new Gray(MAX_INT32), 2);
                    biggestContour = currentContour;


                    hull = biggestContour.GetConvexHull(Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
                    box = biggestContour.GetMinAreaRect();
                    PointF[] points = box.GetVertices();

                    Point[] ps = new Point[points.Length];
                    for (int i = 0; i < points.Length; i++)
                        ps[i] = new Point((int)points[i].X, (int)points[i].Y);

                    currentFrame.DrawPolyline(hull.ToArray(), true, new Gray(MAX_INT32), 2);
                    colorFrame.DrawPolyline(hull.ToArray(), true, new Bgr(Color.White), 2);
                    currentFrame.Draw(new CircleF(new PointF(box.center.X, box.center.Y), 3), new Gray(MAX_INT32), 2);

                    colorFrame.Draw(new CircleF(new PointF(box.center.X, box.center.Y), 3), new Bgr(Color.White), 2);

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


                    PointF dpcpoint;

                    dPointList = new Seq<PointF>(storage);
                    //find depth point circle- eddie
                    for (int i = 0; i < defectArray.Length; i++)
                    {
                        dpcpoint = new PointF((float)defectArray[i].StartPoint.X, (float)defectArray[i].StartPoint.Y);
                        dPointList.Push(dpcpoint);
                    }

                    PointF[] endpointarray = dPointList.ToArray();

                    realendPointList = new Seq<PointF>(storage);
                    for (int i = 0; i < endpointarray.Length; i++)
                    {
                        realendPointList.Push(dPointList[i]);
                    }

                    // convert to depth pointF array
                    PointF[] dpointlistarr = dPointList.ToArray<PointF>();
                    double[] distarr = new double[dpointlistarr.Length];
                    return true;
                }
                else
                    return false;
            }
        }

        private void DrawAndComputeFingersNum()
        {
            /*BlobTrackerAutoParam<Gray> param = new BlobTrackerAutoParam<Gray>();
            param.FGDetector = new FGDetector<Gray>(Emgu.CV.CvEnum.FORGROUND_DETECTOR_TYPE.FGD);
            param.FGTrainFrames = 10;
            BlobTrackerAuto<Gray> tracker = new BlobTrackerAuto<Gray>(param);*/


            int fingerNum = 0;

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
                    if ((startPoint.Y > box.center.Y) && (Math.Sqrt(Math.Pow(startCircle.Center.X - depthCircle.Center.X, 2) + Math.Pow(startCircle.Center.Y - depthCircle.Center.Y, 2)) > box.size.Height / 12))
                    {
                        fingerNum++;
                        colorFrame.Draw(startCircle, new Bgr(Color.Red), 2);
                        colorFrame.Draw(depthCircle, new Bgr(Color.Pink), 2);
                    }
                    else
                    {
                        colorFrame.Draw(startCircle, new Bgr(Color.DarkBlue), 3);
                    }

                }
                if (realendPointList.Total > 0)
                {
                    PointF[] temp = realendPointList.ToArray();
                    for (int i = 0; i < temp.Length; i++)
                    {
                        CircleF startCircle2 = new CircleF(temp[i], 5f);
                        colorFrame.Draw(startCircle2, new Bgr(Color.Red), 2);
                    }
                }
            }
            #endregion

            if(mouse.AddFrame(dPointList, fingerNum, shapeContour)) 
                dataOutput.Text = "watching";
            else
                dataOutput.Text = "not watching";

            MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_DUPLEX, 5d, 5d);
            colorFrame.Draw(fingerNum.ToString(), ref font, new Point(50, 150), new Bgr(255, 10, 10));
        }

        private void bRecalibrate_Click(object sender, EventArgs e)
        {
            recalibrate = true;
        }
    }
}