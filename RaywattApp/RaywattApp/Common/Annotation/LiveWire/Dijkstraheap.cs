using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.SymbolStore;
using PointF = System.Drawing.PointF;

namespace RaywattApp.Common.Annotation.LiveWire
{
    public class DijkstraHeap
    {
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

        public List<PointF> line;   //점, 선 정보를 저장 (System.Drawing.Point)
        public List<PointF> clickPoint; //ClickPosition 정보를 저장 (System.Drawing.Point)
        public List<PointF> trackedPoint;

        // converts x, y coordinates to vector index
        private int toIndex(int x, int y)
        {
            return (y * width + x);
        }

        //initializes Dijkstra with the image
        // parameter : image data, width, height
        public DijkstraHeap(byte[] image, int x, int y)
        {
            line = new List<PointF>();
            clickPoint = new List<PointF>();
            trackedPoint = new List<PointF>();

            //initializes weights for edge cost
            //these are default values
            // 최단 경로 계산에 사용되는 가중치 값
            gradientMagnitude = 0.43; // 경로 weight(거리) 가중치
            exponentialWeight = 1; // 경로 edge(간선) 가중치
            potenceWeight = 30; // 경로 pixel 가중치

            // initializes all other matrices
            imagePixels = new int[x * y];
            //	imageCosts  = new int [x*y];
            // 경로 cost(비용) 저장(우선순위 큐 이용)
            pixelCosts = new PriorityQueue<PixelNode>();
            // 이전 픽셀의 인덱스 저장
            whereFrom = new int[x * y];
            // 방문 여부
            visited = new bool[x * y];
            width = x;
            height = y;

            // for debug reasons
            // used to store
            pCosts = new double[x][];

            for (int i = 0; i < x; i++)
            {
                pCosts[i] = new double[y];
            }

            gradientr = gradientx = gradienty= new double[x*y];
            // copy image matrice
            for (int j = 0; j < y; j++)
            {
                for (int i = 0; i < x; i++)
                {
                    imagePixels[j * x + i] = (int)image[j * x + i];
                    visited[j * x + i] = false;
                    gradientr[j * x + i] = gradientx[j * x + i] = gradienty[j * x + i] = (double)image[j * x + i];
                    grmax = 255;
                    grmin = 0;
                }
            }
        }

        //returns de cost of going from sx,sy to dx,dy
        // 입력 : 시작점, 끝점
        // output : 가중치를 적용하여 edge값 계산
        private double edgeCost(int sx, int sy, int dx, int dy)
        {
            //fg is the Gradient Magnitude

            //we are dividing by sqrt(2) so that the value won't pass 1
            //as is stated in United Snakes formule 36
            double fg = (1.0 / Math.Sqrt(2) * 
                Math.Sqrt((dx - sx) * (dx - sx) + (dy - sy) * (dy - sy)) *
                (1 - ((gradientr[toIndex(dx, dy)] - grmin) / (grmax - grmin))));
            if (grmin == grmax)
                fg = (1.0 / Math.Sqrt(2) * Math.Sqrt((dx - sx) * (dx - sx) + (dy - sy) * (dy - sy)));

            //this parameter is an attempt to find edges in IVUS images	
            double x = (gradientr[toIndex(dx, dy)] - grmin) / (grmax - grmin);
            double fe = Math.Exp(-potenceWeight * x) * fg;

            return exponentialWeight * fe + gradientMagnitude * fg + 0.05*Math.Sqrt((dx - sx) * (dx - sx) + (dy - sy) * (dy - sy)); 

        }

        private void updateCosts(int x, int y, double mycost)
        {

            // 방문 표시
            visited[toIndex(x, y)] = true;
            // 가장 작은 cost를 가진 노드 제거
            if (pixelCosts.Count > 0)
            {
                pixelCosts.Dequeue();
            }

            // 주변 8개의 노드 cost 갱신(edgeCost 사용)
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
        public int returnPath(int x, int y, int[] vx, int[] vy, out int length, int [] pixelValue)
        {
            //returns the path given mouse position
            //System.out.println(pCosts[x][y] + " my cost " + gradientr[toIndex(x,y)]+ " grmin " + grmin + " grmax " + grmax);

            int[] tempx = new int[width * height];
            int[] tempy = new int[width * height];

            // visited = false => 아직 탐색X => 경로를 찾을 수 없는 위치
            if (visited[toIndex(x, y)] == false)
            {
                //attempt to get path before creating it 
                length = 0;
                return 0;
            }

            //add points to vector
            int myx = x; // 현재 위치
            int myy = y;
            int nextx; // 다음 위치
            int nexty;
            int count = 0;
            tempx[0] = myx;//add last points
            tempy[0] = myy;
            //	System.out.println("Caminho ");
            do
            { //while we haven't found the seed	    	
                nextx = whereFrom[toIndex(myx, myy)] % width;
                nexty = whereFrom[toIndex(myx, myy)] / width;
                //System.out.println("("+nextx+","+nexty+")");

                count++;
                tempx[count] = nextx;
                tempy[count] = nexty;

                myx = nextx;
                myy = nexty;

            } while (!((myx == sx) && (myy == sy)));

            length = count;
            //path is from last point to first
            //we need to invert it
            //	System.out.println("Caminho ");
            for (int i = 0; i <= count; i++)
            {
                vx[i] = tempx[count - i]; // 역순
                vy[i] = tempy[count - i];
                pixelValue[i] = imagePixels[toIndex(tempx[count - i], tempy[count - i])];
                //System.out.println("( "+vx[i] + " , " + vy[i] + " )");
            }

            sx = x;
            sy = y;

            return count;

        }

                
        public void run(int x, int y)
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

            visited[toIndex(x, y)] = true; 

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


                pCosts[nextX][nextY] = ((PixelNode)pixelCosts.Peek()).GetDistance();


                updateCosts(nextX, nextY, ((PixelNode)pixelCosts.Peek()).GetDistance());

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
