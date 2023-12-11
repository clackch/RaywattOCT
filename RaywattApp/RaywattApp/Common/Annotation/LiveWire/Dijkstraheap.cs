using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using Point = System.Windows.Point;
using log4net;
using RaywattApp.ViewModels;

namespace RaywattApp.Common.Annotation.LiveWire
{
    public class DijkstraHeap
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DijkstraHeap));
        private int[] imagePixels; // stores Pixels from original image
        PriorityQueue<PixelNode> pixelCosts;
        double[] gradientx = new double[0]; // stores image gradient modulus 
        double[] gradienty = new double[0]; // stores image gradient modulus 
        public double[] gradientr = new double[0]; // stores image gradient RESULTANT modulus 
        double grmin;// gradient global minimum
        public double grmax;// gradient global maximum

        int[] whereFrom;  // stores where from path started
        public bool[] visited; // stores whether the node was marked or not
        public int width;
        public int height;
        public int sx = -1, sy = -1; // seed x and seed y, weight zero for this point
        
        private double gradientMagnitude;// Gradient Magnitude Weight - set by setGWeight
        private double exponentialWeight;// Exponential Weight - set by setEWeight
        private double potenceWeight;// Exponential Potence Weight - set by setPWeight

        double[][] pCosts;// for debugging reasons

        public List<List<Point>> line;
        public List<Point> trackPoint;
        // converts x, y coordinates to vector index
        private int toIndex(int x, int y)
        {
            return (y * width + x);
        }

        //initializes Dijkstra with the image
        // parameter : image data, width, height
        public DijkstraHeap(byte[] image, int x, int y)
        {
            line = new List<List<Point>>();
            for (int i = 0; i<9; i++) // Track Point 개수 10개로 제한. 즉, 구간(line)은 총 9개
            {
                line.Add(new List<Point>());
            }
            trackPoint = new List<Point>();

            // 최단 경로 계산에 사용되는 가중치 값
            gradientMagnitude = 0.43; // 경로 weight(거리) 가중치
            exponentialWeight = 1; // 경로 edge(간선) 가중치
            potenceWeight = 30; // 경로 pixel 가중치

            imagePixels = new int[x * y];
            pixelCosts = new PriorityQueue<PixelNode>();
            whereFrom = new int[x * y];
            visited = new bool[x * y];
            width = x;
            height = y;

            gradientr = gradientx = gradienty= new double[x*y];
            for (int j = 0; j < y; j++)
            {
                for (int i = 0; i < x; i++)
                {
                    imagePixels[j * x + i] = (int)image[j * x + i];
                    visited[j * x + i] = false;
                    gradientr[j * x + i] = gradientx[j * x + i] = gradienty[j * x + i] = (double)image[j * x + i];
                }
            }
        }

        // 입력 : 시작점, 끝점
        // output : 가중치를 적용하여 edge값 계산
        private double edgeCost(int sx, int sy, int dx, int dy)
        {
            double fg = 0;
            double distance = Math.Abs(dx - sx) + Math.Abs(dy - sy) == 2 ? Math.Sqrt(2) : 1;
            if (gradientr[toIndex(dx,dy)] != 255) {
                fg = distance;
            }
            return fg + 0.10 * distance; // Grey 0 : Grey 255 = 11 : 1.
        }

        private void updateCosts(int x, int y, double mycost)
        {
            visited[toIndex(x, y)] = true;
            if (pixelCosts.Count > 0)
            {
                pixelCosts.Dequeue();
            }

            //upper right
            if ((x < width - 1) && (y > 0))
            {
                pixelCosts.Enqueue(new PixelNode(toIndex(x + 1, y - 1), mycost + edgeCost(x, y, x + 1, y - 1), toIndex(x, y)));
            }
            //upper left
            if ((x > 0) && (y > 0))
            {
                pixelCosts.Enqueue(new PixelNode(toIndex(x - 1, y - 1), mycost + edgeCost(x, y, x - 1, y - 1), toIndex(x, y)));
            }
            //down right
            if ((x < width - 1) && (y < height - 1))
            {
                pixelCosts.Enqueue(new PixelNode(toIndex(x + 1, y + 1), mycost + edgeCost(x, y, x + 1, y + 1), toIndex(x, y)));
            }
            //down left
            if ((x > 0) && (y < height - 1))
            {
                pixelCosts.Enqueue(new PixelNode(toIndex(x - 1, y + 1), mycost + edgeCost(x, y, x - 1, y + 1), toIndex(x, y)));
            }

            //update left cost
            if (x > 0)
            {
                pixelCosts.Enqueue(new PixelNode(toIndex(x - 1, y), mycost + edgeCost(x, y, x - 1, y), toIndex(x, y)));
            }
            //update right cost
            if (x < width - 1)
            {
                pixelCosts.Enqueue(new PixelNode(toIndex(x + 1, y), mycost + edgeCost(x, y, x + 1, y), toIndex(x, y)));
            }

            //update up cost
            if (y > 0)
            {
                pixelCosts.Enqueue(new PixelNode(toIndex(x, y - 1), mycost + edgeCost(x, y, x, y - 1), toIndex(x, y)));
            }
            //update down cost
            if (y < height - 1)
            {
                pixelCosts.Enqueue(new PixelNode(toIndex(x, y + 1), mycost + edgeCost(x, y, x, y + 1), toIndex(x, y)));
            }
        }

        // 시작점 : sx, sy
        // 마우스 위치(종착점) : x, y
        // 종착점으로 부터 부분 경로 반환 : vx, vy
        // 경로 길이 : mylength
        public void returnPath(int endX, int endY, int[] vx, int[] vy, out int length, int [] pixelValue)
        {
            if (visited[toIndex(endX, endY)] == false)
            {
                length = 0;
                return; // 경로가 너무 짧을 때 오류처리 할 수 있을 듯.
            }

            int myx = endX; // 현재 위치
            int myy = endY;
            int nextx; // 다음 위치
            int nexty;
            int count = 0;
            do
            {
                nextx = whereFrom[toIndex(myx, myy)] % width;
                nexty = whereFrom[toIndex(myx, myy)] / width;

                vx[count] = nextx;
                vy[count] = nexty;
                pixelValue[count] = imagePixels[toIndex(vx[count], vy[count])];

                count++;
                myx = nextx;
                myy = nexty;

            } while (!((myx == sx) && (myy == sy)));

            length = count;
            sx = endX;
            sy = endY;
        }

                
        public void run(int x, int y, int dx, int dy)
        {
            int nextIndex;
            int nextX;
            int nextY;

            sx = x;
            sy = y;

            for (int i = 0; i < height * width; i++)
            {
                visited[i] = false;
            }
            
            // init last point
            whereFrom[toIndex(x, y)] = toIndex(x, y);

            //init costs
            updateCosts(x, y, 0);

            while ((pixelCosts.Count > 0))
            {
                nextIndex = ((PixelNode)pixelCosts.Peek()).GetIndex();
                nextX = nextIndex % width;
                nextY = nextIndex / width;

                whereFrom[nextIndex] = ((PixelNode)pixelCosts.Peek()).GetWhereFrom();
                                
                updateCosts(nextX, nextY, ((PixelNode)pixelCosts.Peek()).GetDistance());

                if (nextX == dx && nextY == dy)
                {
                    break;
                }

                //removes pixels that are already visited and went to the queue
                while (true)
                {
                    if (pixelCosts.Peek() == null)
                        break;
                    if (visited[((PixelNode)pixelCosts.Peek()).GetIndex()] == false)
                        break;
                    pixelCosts.Dequeue();
                }
            }
            while (pixelCosts.Count > 0)
                pixelCosts.Dequeue();
        }
    }

    public class PixelNode : IComparable<PixelNode>
    {
        private int myIndex;
        private double myDistance;
        private int whereFrom;

        public PixelNode(int index, double distance, int whereFrom)
        {
            myIndex = index;
            myDistance = distance;
            this.whereFrom = whereFrom;
        }

        public double GetDistance()
        {
            return myDistance;
        }

        public int GetIndex()
        {
            return myIndex;
        }

        public int GetWhereFrom()
        {
            return whereFrom;
        }

        public int CompareTo(PixelNode? other)
        {
            if (other == null)
            {
                return 1;
            }

            if (myDistance < other.GetDistance())
            {
                return -1;
            }
            else if (myDistance > other.GetDistance())
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

    }
}
