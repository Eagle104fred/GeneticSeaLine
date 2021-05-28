using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
namespace CaptureSeaLine
{
    class ScreenProportion
    {
        private Queue<double2> proportionQueue = new Queue<double2>();

        
         public void AddLine(double2 line)
         {
            int size=proportionQueue.Count();

            for(int i=0;i<size;i++)
            {
                double2 getLine=proportionQueue.Dequeue();
                if (!(line[0] > getLine[1] || getLine[0] > line[1])) //KS:min>max的情况会被排除 
                {
                    line = new double2(math.min(getLine[0], line[0]), math.max(getLine[1], line[1]));  //KS:延长自身的线段,把原来的取出丢掉 
                }
                else
                {
                    proportionQueue.Enqueue(getLine);  //KS:无法延长,塞回去 
                }
            }
            proportionQueue.Enqueue(line);

        }

        public double GetLenghtAll()
        {
            double totalLenght = 0;
            int size = proportionQueue.Count();
            for(int i=0;i<size;i++)
            {
                double2 tempLine = proportionQueue.Dequeue();
                totalLenght += (tempLine.y - tempLine.x);
            }
            return totalLenght;
        }

        public void Clear()
        {
            proportionQueue.Clear();
        }
    }
}
