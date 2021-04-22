#define VIDEO
using OpenCvSharp;
using System;
using System.Threading.Tasks;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Randomizations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using Statistics.Models;


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
                               
                    
                while (Capture.Read(img))
                {
                    Mat imgGray = new Mat();
                    Cv2.CvtColor(img, imgGray, ColorConversionCodes.BGR2GRAY);
                    Cv2.MedianBlur(imgGray, imgGray, 9);
                    Cv2.Threshold(imgGray, imgGray, 0, 255, ThresholdTypes.Otsu);
                    //Cv2.Threshold(imgGray, imgGray, 100, 250, ThresholdTypes.Tozero);

                    Cv2.Canny(imgGray, imgGray, 10, 50, 3);
                    LineSegmentPoint[] linePoint = Cv2.HoughLinesP(imgGray, 1.0, Cv2.PI / 180, 150, 300, 100);
                    
                    if(linePoint.Length>1)
                    {
                        GeneticOptimizor GA = new GeneticOptimizor();
                        GA.AddLine(linePoint);
                        GA.Run();
                        Cv2.Line(img, GA.startPoint, GA.endPoint, Scalar.Red, 4);
                    }
                    
                    
                    for (int i = 0; i < linePoint.Length; i++)
                    {
                        Point p1 = linePoint[i].P1;
                        Point p2 = linePoint[i].P2;
                        Cv2.Line(img, p1, p2, Scalar.Green, 4);
                    }
                    //Cv2.ImShow("test", imgGray);
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
