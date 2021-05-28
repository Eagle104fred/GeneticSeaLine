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
using Statistics.Models;

namespace CaptureSeaLine
{
    class GeneticOptimizor
    {
        List<Point> pointList = new List<Point>();
        public GeneticAlgorithm ga;
        public Point startPoint;
        public Point endPoint;
        const int screenWidth = 1920;

        public void AddLine(LineSegmentPoint[] linePointArr)
        {
            for (int i = 0; i < linePointArr.Length; i++)
            {
                var point1 = linePointArr[i].P1;
                var point2 = linePointArr[i].P2;
                pointList.Add(point1);
                pointList.Add(point2);
            }
        }

        public void Run()
        {
            var selection = new EliteSelection();
            var crossover = new TwoPointCrossover();
            var mutation = new MyMutation();
            var fitness = new MyProblemFitness(pointList.ToArray());
            var chromosome = new MyProblemChromosome(pointList.Count);
            var population = new Population(100, 100, chromosome);

            ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
            {
                Termination = new FitnessStagnationTermination(100),
                MutationProbability = 0.5f, //KS:变异概率50% 
                CrossoverProbability = 0.5f, //KS:交配概率50% 
            };

            int index = 0; //KS:计算代数 

            ga.GenerationRan += delegate
            {
                var bestChromosome = ga.Population.BestChromosome;

                Console.Write("Index: " + index);
                Console.Write(", Fitness: {0}", bestChromosome.Fitness);

                Console.Write(", Genes: {0}", string.Join("-", bestChromosome.GetGenes()));

                Console.WriteLine();

                index++;
            };


           
            Console.WriteLine("GA running...");
            Stopwatch SW = new Stopwatch();
            SW.Start();
           
            ga.Start();//调用GA线程

            
            SW.Stop();
            //Console.Write(", Time: {0}", SW.ElapsedMilliseconds);
            //Console.WriteLine("Best solution found has {0} fitness.", ga.BestChromosome.Fitness);
            fitness.Evaluate(ga.BestChromosome); //KS: 评价基因的优劣
           
  
            startPoint = new Point(0,(double)fitness.lr.Alpha);
            endPoint = new Point(screenWidth, (double)(fitness.lr.Alpha+screenWidth*fitness.lr.Beta));

        }

    }
    public class MyProblemFitness : IFitness
    {
        private Point[] pointArr;

        public LinearRegression lr;
        public MyProblemFitness(Point[] p)
        { 
            pointArr = p; 
        }

        const double MinLimitScoreRadio = 0.01;
        public double Evaluate(IChromosome chromosome)
        {
            var genes = chromosome.GetGenes();

            List<Point> getPointList = new List<Point>();
            for (int i = 0; i < genes.Length; i++)
            {

                if ((int)genes[i].Value == 1)
                {
                    getPointList.Add(pointArr[i]);
                }

            }
            if (getPointList.Count <= 1) return double.MinValue;


            double Score = Math.Pow(10, -getPointList.Count); //KS:使得取到的点越多越好 
                                                              //Console.Write(""+ Score+" "+getPointList.Count+" ");
            decimal[] x = new decimal[getPointList.Count];
            decimal[] y = new decimal[getPointList.Count];
            decimal diffx = 0;
            decimal diffy = 0;
            for (int i = 0; i < getPointList.Count; i++)
            {
                x[i] = getPointList[i].X;
                y[i] = getPointList[i].Y;
                //Console.Write(" x:"+x[i]+" y:"+y[i] );
                if (i >= 1)
                {
                    diffx += Math.Abs(x[i] - x[i - 1]);
                    diffy += Math.Abs(y[i] - y[i - 1]);
                }
            }
            //KS:防止斜率为0或者无穷大  
            if (diffx <= 1E-5m || diffy <= 1E-5m)
            {
                return -Score * MinLimitScoreRadio;
            }

            lr = new LinearRegression();

            lr.Compute(y, x);

            var b = lr.Alpha;
            var k = lr.Beta;
            decimal total = 0;
            for (int i = 0; i < getPointList.Count; i++)
            {
                decimal temp = (getPointList[i].X * k + b) - getPointList[i].Y;
                total += temp * temp;
            }
            /* double RSquared = (double)lr.RSquared;
             Console.WriteLine(" " + lr.RSquared + " " + lr.RValue);*/
            Score = Score * Math.Max((double)total, MinLimitScoreRadio);

            return -Score;
        }
    }

    public class MyProblemChromosome : ChromosomeBase
    {
        public MyProblemChromosome(int length) : base(length)
        {
            CreateGenes();
        }

        public override Gene GenerateGene(int geneIndex)
        {
            var rnd = RandomizationProvider.Current;

            return new Gene(rnd.GetInt(0, 2));
        }

        public override IChromosome CreateNew()
        {
            return new MyProblemChromosome(Length);
        }
    }
}
