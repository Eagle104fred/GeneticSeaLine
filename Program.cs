#define VIDEO
using OpenCvSharp;


namespace CaptureSeaLine
{
    

    class Program
    {
        static string rtspUrl = "rtsp://admin:SMUwm_007@183.192.69.170:7502/id=1";

        static void Main(string[] args)
        {
            
            VideoCapture Capture = new VideoCapture(rtspUrl);
            Cv2.NamedWindow("test", WindowFlags.Normal);
            Cv2.NamedWindow("org", WindowFlags.Normal);

#if VIDEO
            if (Capture.IsOpened())
            {

                Mat img = new Mat();
                Point startPoint = new Point(0, 0);
                Point endPoint = new Point(1920, 0);
                double minFitness = double.MinValue;
                while (Capture.Read(img))
                {
                    
                    Mat imgGray = new Mat();

                    Cv2.CvtColor(img, imgGray, ColorConversionCodes.BGR2GRAY); //KS:灰度化 
                    Rect roi = new Rect(0, 0, 1920, 120);
                    Cv2.Rectangle(imgGray, roi, Scalar.White, -1); //KS:剔除摄像机字段信息 


                    Cv2.MedianBlur(imgGray, imgGray, 9); //KS:中值滤波 
                    Cv2.Threshold(imgGray, imgGray, 0, 255, ThresholdTypes.Otsu); //KS:二值化 
                    //Cv2.Threshold(imgGray, imgGray, 100, 250, ThresholdTypes.Tozero);

                    Cv2.Canny(imgGray, imgGray, 10, 50, 3); //KS:边沿提取 
                    //KS: 霍夫直线提取-3:一条直线累加的平面阈值;-2:过滤比这个短的线;-1:点与点之间的间隔
                    LineSegmentPoint[] linePoint = Cv2.HoughLinesP(imgGray, 1.0, Cv2.PI / 180, 100, 300, 100);

                    //KS:点的计算方法 
                    /*if (linePoint.Length > 1)
                    {
                        GeneticOptimizor GA = new GeneticOptimizor();
                        GA.AddLine(linePoint);
                        GA.Run(); //KS:遗传算法 
                        Cv2.Line(img, GA.startPoint, GA.endPoint, Scalar.Red, 4);
                    }*/
                    //KS:线的计算方法 
                        GeneticOptimizor2 GA = new GeneticOptimizor2();
                        GA.AddLine(linePoint);
                        GA.Run(ref minFitness, ref startPoint, ref endPoint); //KS:遗传算法 
                        Cv2.Line(img, startPoint, endPoint, Scalar.Red, 4);
                    

                    for (int i = 0; i < linePoint.Length; i++)
                    {
                        Point p1 = linePoint[i].P1;
                        Point p2 = linePoint[i].P2;
                        Cv2.Line(img, p1, p2, Scalar.Green, 4);
                    }

                    Cv2.ImShow("org", img);
                    Cv2.ImShow("test", imgGray);
                    Cv2.WaitKey(1);
                }
                    
                    
                
                
            }
#else
            Mat img = Cv2.ImRead(@"D:\C#\Code\CaptureSeaLine\bin\Debug\SeaLine.png", ImreadModes.Color);
            Mat imgGray = new Mat();
            Cv2.CvtColor(img, imgGray, ColorConversionCodes.BGR2GRAY);
            Cv2.MedianBlur(imgGray, imgGray, 9);
            Cv2.Threshold(imgGray, imgGray, 0, 255, ThresholdTypes.Otsu);
            //Cv2.Threshold(imgGray, imgGray, 100, 250, ThresholdTypes.Tozero);

            Cv2.Canny(imgGray, imgGray, 10, 50, 3);
            LineSegmentPoint[] linePoint = Cv2.HoughLinesP(imgGray, 1.0, Cv2.PI / 180, 200, 300, 100);
            for (int i = 0; i < linePoint.Length; i++)
            {
                Point p1 = linePoint[i].P1;
                Point p2 = linePoint[i].P2;
                Cv2.Line(img, p1, p2, Scalar.Green, 4);
            }
            Cv2.ImShow("test", imgGray);
            Cv2.ImShow("org", img);
            Cv2.WaitKey(0);
#endif
        }

    }

    
}
