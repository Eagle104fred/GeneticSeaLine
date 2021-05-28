using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
using OpenCvSharp;
using Unity.Mathematics;

namespace OpenCvSharp
{
    static class MathExt
    {
        public static double2 ToDouble2(this Point point)
        {
            return new double2(point.X, point.Y);
        }

        public static double2x2 ToDouble2x2(this LineSegmentPoint line)
        {
            return new double2x2(line.P1.ToDouble2(), line.P2.ToDouble2());
        }

        public static Point ToPoint(this double2 point)
        {
            return new Point((int)point.x, (int)point.y);
        }

        public static LineSegmentPoint ToLine(this double2x2 line)
        {
            return new LineSegmentPoint(line.c0.ToPoint(), line.c1.ToPoint());
        }
    }
}

namespace CaptureSeaLine
{
    class GeneticOptimizor2
    {
        public MyProblemFitness2 fitness;
        public GeneticAlgorithm ga;

        public const int screenWidth = 1920; //KS:视频宽度 
        LineSegmentPoint[] linePointArr;


        public void AddLine(LineSegmentPoint[] linePointArr)
        {
            this.linePointArr = linePointArr;

        }

        public void Run(ref double Minfitness, ref Point startPoint, ref Point endPoint)
        {

            if (linePointArr.Length == 0)
            {
                return;
            }
            else
            {
                var selection = new EliteSelection();
                var crossover = new TwoPointCrossover();
                var mutation = new MyMutation();
                fitness = new MyProblemFitness2(linePointArr, startPoint, endPoint);
                var chromosome = new MyProblemChromosome2(linePointArr.Length < 3 ? 3 : linePointArr.Length);
                var population = new Population(linePointArr.Length + 3, linePointArr.Length + 3, chromosome);

                ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
                {
                    Termination = new TimeEvolvingTermination(TimeSpan.FromMilliseconds(100)),
                    MutationProbability = 2f, //KS:变异概率50% 
                    CrossoverProbability =0.5f, //KS:交配概率50% 
                };

                int index = 0; //KS:计算代数 

                ga.GenerationRan += delegate
                {
                    var bestChromosome = ga.Population.BestChromosome;

                    /*                Console.Write("Index: " + index);
                                    Console.Write(", Fitness: {0}", bestChromosome.Fitness);

                                    Console.Write(", Genes: {0}", string.Join("-", bestChromosome.GetGenes()));

                                    Console.WriteLine();*/

                    index++;
                };

                /*            Console.WriteLine("GA running...");
                            Stopwatch SW = new Stopwatch();
                            SW.Start();
                           */
                ga.Start();//调用GA线程


                // SW.Stop();
                //Console.Write(", Time: {0}", SW.ElapsedMilliseconds);
                Console.WriteLine("Best solution found has {0} fitness : {1}.", string.Join("-", ga.Population.BestChromosome.GetGenes()), ga.Population.BestChromosome.Fitness);

            }

            fitness.Evaluate(ga.Population.BestChromosome); //KS: 评价基因的优劣
            Minfitness = ga.Population.BestChromosome.Fitness.Value;
            startPoint = fitness.start.ToPoint();
            endPoint = fitness.end.ToPoint();
            /*
            if (ga.Population.BestChromosome.Fitness > Minfitness)
            {
                Minfitness = ga.Population.BestChromosome.Fitness.Value;
                startPoint = fitness.start.ToPoint();
                endPoint = fitness.end.ToPoint();
            }
            else
            {
                Minfitness = math.lerp((float)Minfitness, (float)ga.Population.BestChromosome.Fitness, 0.5f);
            }
            */
        }
        public void CheckAnswer()
        {
            fitness.Evaluate(ga.Population.BestChromosome); //KS: 评价基因的优劣
            var fitnessvalue = ga.Population.BestChromosome.Fitness.Value;
            var startPoint = fitness.start.ToPoint();
            var endPoint = fitness.end.ToPoint();
        }
    }

    public class MyProblemFitness2 : IFitness
    {
        private double2x2[] lineArr; //KS:存储向量 
        //KS:最终海天线的起终点  
        public double2 start;
        public double2 end;
        //KS:保存上一帧的起终点 
        public double2 oldStart;
        public double2 oldEnd;
        private ScreenProportion screenProportion = new ScreenProportion();
        public MyProblemFitness2(LineSegmentPoint[] p, Point oldStart, Point oldEnd)
        {

            lineArr = p.Select(x => x.ToDouble2x2()).ToArray(); //KS:拿出数组的每条线段 
            this.oldStart = oldStart.ToDouble2();
            this.oldEnd = oldEnd.ToDouble2();
        }
        public double Evaluate(IChromosome chromosome)
        {
            screenProportion.Clear();
            var genes = chromosome.GetGenes();



            //OYM:计算斜率
            double2 resultVector = 0;
            double2[] targetVector = new double2[genes.Length];
            for (int i = 0; i < lineArr.Length; i++)
            {
                if ((int)genes[i].Value==1)
                {
                    screenProportion.AddLine(new double2(lineArr[i].c0.x, lineArr[i].c1.x)); //KS:计算线段在x轴的覆盖值 

                    targetVector[i] = lineArr[i].c1 - lineArr[i].c0;
                    resultVector += targetVector[i];//KS:向量首尾相连 
                }
            }
            if (math.lengthsq(resultVector) == 0)
            {
                return double.MinValue;  //KS:防止全不选的情况 
            }

            double2 vectorNormal = math.normalize(resultVector); //OYM:获取map的x轴
            double k = vectorNormal.y / vectorNormal.x; //KS: 算出map坐标x轴在真值坐标的斜率
            double2 vectorverticalNormal = math.cross(new double3(0, 0, 1), new double3(vectorNormal, 0)).xy; //OYM:获取map坐标系的y轴

            double2[] mapData = new double2[genes.Length];
            double sumxy = 0;
            double sumx = 0;
            double[] mapVectorLengthX = new double[genes.Length];
            //KS:把所有线段换算到map坐标下 
            for (int i = 0; i < lineArr.Length; i++)
            {
                if ((int)genes[i].Value == 1)
                {
                    //KS:vectorNormal是map坐标x轴单位向量点积后就相当于把线段的长度映射到map.x
                    mapVectorLengthX[i] = math.dot(vectorNormal, targetVector[i]);
                    //KS:计算线段中点的map坐标 
                    double2 targetCenter = (lineArr[i].c0 + lineArr[i].c1) / 2;
                    mapData[i].x = math.dot(vectorNormal, targetCenter);
                    double2 X = vectorNormal * mapData[i].x; //KS:转换为真实坐标 
                    mapData[i].y = math.distance(X, targetCenter);
                    //KS: 线性回归
                    sumxy += mapVectorLengthX[i] * mapData[i].y;
                    sumx += mapVectorLengthX[i];
                }
            }
            //OYM:面积的线性回归

            double yhat = sumxy / sumx;//OYM:计算目标线
            double2 mapStart = vectorverticalNormal * yhat;
            start = new double2(0, mapStart.y - k * mapStart.x); //KS:真正坐标系下yhat的坐标 

            end = new double2(GeneticOptimizor2.screenWidth, start.y + k * GeneticOptimizor2.screenWidth);
            //OYM:评价
            //double Score = sumx; //OYM:线段在海天线上投影的长度之和
            double Score = screenProportion.GetLenghtAll();

            for (int i = 0; i < lineArr.Length; i++)
            {
                if ((int)genes[i].Value == 1)
                {
                    double deltaY = math.abs(mapData[i].y - yhat); //KS:与target围成面积的高 
                    double area = mapVectorLengthX[i] * deltaY;//OYM:线段到海天线的投影与垂直的线围城的面积
                    //KS:当线段与海天线的夹角接近90°的时候,也会出现特别小的面积
                    //所以为了惩罚这种情况,我们需要引入一个角度的惩罚
                    //KS:原来的三角形计算面积为ab sin t,现在改为ab*sin t/cos t ,作为一个惩罚的机制

                    //double cosAngle = math.dot(vectorNormal, math.normalize(targetVector[i]));            
                    double cosAngle = mapVectorLengthX[i] / math.length(targetVector[i]);  //KS:这段代码与上面意思相同 
                    double scoreTemp = area / cosAngle;  //KS:计算ab*sin t/cos t 
                    Score -= math.sqrt(scoreTemp);  //KS:取面积 
                }
            }
            //OYM:考虑到防跳变,增加一个屏幕跳变的参数
            // Score -= math.distance(oldStart, start) + math.distance(oldEnd, end); //OYM:防止跳变
            return Score;
        }
    }

    public class MyProblemChromosome2 : BinaryChromosomeBase
    {
        public MyProblemChromosome2(int length) : base(length)
        {
            CreateGenes();
        }

        public override IChromosome CreateNew()
        {
            return new MyProblemChromosome2(Length);
        }


    }
    public class MyMutation : MutationBase
    {

        private readonly IRandomization m_rnd;
        public MyMutation()
        {
            m_rnd = RandomizationProvider.Current;
        }

        protected override void PerformMutate(IChromosome chromosome, float probability)
        {
            var binaryChromosome = chromosome as IBinaryChromosome;

            if (binaryChromosome == null)
            {
                throw new MutationException(this, "Needs a binary chromosome that implements IBinaryChromosome.");
            }
            double temp;
            while ((temp= m_rnd.GetDouble()) <= probability)
            {
                probability -=(float)temp;
                var index = m_rnd.GetInt(0, chromosome.Length);
                binaryChromosome.FlipGene(index);
            }
        }
    }
}