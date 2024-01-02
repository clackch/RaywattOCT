using System;
using System.Collections.Generic;
using Point = System.Windows.Point;
using log4net;

namespace RaywattApp.Common.Angio
{
    public class DijkstraHeap
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DijkstraHeap));
        private int[] imagePixels; // stores Pixels from original image
        PriorityQueue<PixelNode, double> pixelCosts;
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
        private double sqrt2 = 1.41421356237;
        // converts x, y coordinates to vector index
        private int toIndex(int x, int y)
        {
            return y * width + x;
        }

        //initializes Dijkstra with the image
        // parameter : image data, width, height
        public DijkstraHeap(byte[] image, int x, int y)
        {
            line = new List<List<Point>>();
            for (int i = 0; i < 9; i++) // Track Point 개수 10개로 제한. 즉, 구간(line)은 총 9개
            {
                line.Add(new List<Point>());
            }
            trackPoint = new List<Point>();

            // 최단 경로 계산에 사용되는 가중치 값
            gradientMagnitude = 0.43; // 경로 weight(거리) 가중치
            exponentialWeight = 1; // 경로 edge(간선) 가중치
            potenceWeight = 30; // 경로 pixel 가중치

            imagePixels = new int[x * y];
            pixelCosts = new PriorityQueue<PixelNode, double>();
            whereFrom = new int[x * y];
            visited = new bool[x * y];
            width = x;
            height = y;

            gradientr = gradientx = gradienty = new double[x * y];
            for (int j = 0; j < y; j++)
            {
                for (int i = 0; i < x; i++)
                {
                    imagePixels[j * x + i] = image[j * x + i];
                    visited[j * x + i] = false;
                    gradientr[j * x + i] = gradientx[j * x + i] = gradienty[j * x + i] = image[j * x + i];
                }
            }
        }

        private double edgeCost(int sx, int sy, int dx, int dy)
        {
            double pixelValue = gradientr[toIndex(dx, dy)]; // dx, dy 위치의 픽셀 값
            double maxPixelValue = 255; // 최대 픽셀 값 (예: 255)

            // 픽셀 값에 따른 cost 계산
            // 픽셀 값이 높을수록 낮은 cost 부여
            double cost = maxPixelValue - pixelValue;

            int gap = 0;
            if (sx != dx) gap++;
            if (sy != dy) gap++;

            // 추가적으로 거리에 따른 가중치 적용
            double distance = gap == 2 ? sqrt2 : 1;
            double weight = 50;
            cost += weight * distance; // 거리에 따른 가중치 추가, 최적 weight값 찾을 필요 있음

            return cost;
        }

        private void updateCosts(int x, int y, double mycost)
        {
            if (pixelCosts.Count > 0)
            {
                pixelCosts.Dequeue();
            }

            (int, int)[] directions = new (int, int)[]
            {
                (-1, -1), // upper left
                (1, -1),  // upper right
                (-1, 1),  // down left
                (1, 1),   // down right
                (-1, 0),  // left
                (1, 0),   // right
                (0, -1),  // up
                (0, 1)    // down
            };

            foreach (var (dx, dy) in directions)
            {
                int newX = x + dx;
                int newY = y + dy;

                // 이미지 경계값 확인 및 방문하지 않은 노드인지 확인
                if (newX >= 0 && newX < width && newY >= 0 && newY < height && !visited[toIndex(newX, newY)])
                {
                    double newCost = mycost + edgeCost(x, y, newX, newY);
                    pixelCosts.Enqueue(new PixelNode(toIndex(newX, newY), newCost, toIndex(x, y)), newCost);
                    visited[toIndex(newX, newY)] = true;
                }
            }
        }

        // 시작점 : sx, sy
        // 마우스 위치(종착점) : x, y
        // 종착점으로 부터 부분 경로 반환 : vx, vy
        // 경로 길이 : mylength
        public void ReturnPath(int endX, int endY, int[] vx, int[] vy, out int length, int[] pixelValue)
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

            } while (!(myx == sx && myy == sy));

            length = count;
            sx = endX;
            sy = endY;
        }


        public void CalculatePathCost(int x, int y, int dx, int dy)
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

            pixelCosts.Clear();

            // init last point
            whereFrom[toIndex(x, y)] = toIndex(x, y);

            //init costs
            updateCosts(x, y, 0);
            visited[toIndex(x, y)] = true;

            while (pixelCosts.Count > 0)
            {
                nextIndex = pixelCosts.Peek().GetIndex();
                nextX = nextIndex % width;
                nextY = nextIndex / width;

                whereFrom[nextIndex] = pixelCosts.Peek().GetWhereFrom();

                updateCosts(nextX, nextY, pixelCosts.Peek().GetDistance());

                if (nextX == dx && nextY == dy)
                {
                    break;
                }
            }
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

        public double GetDistance() => myDistance;
        public int GetIndex() => myIndex;
        public int GetWhereFrom() => whereFrom;

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
