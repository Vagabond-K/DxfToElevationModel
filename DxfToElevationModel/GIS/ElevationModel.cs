using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace DxfToElevationModel.GIS
{
    /// <summary>
    /// 표고 모델
    /// </summary>
    public class ElevationModel
    {
        class ContourLineSegment
        {
            public ContourLineSegment(ContourLinePoint point1, ContourLinePoint point2, double elevation)
            {
                Point1 = point1;
                Point2 = point2;
                Elevation = elevation;
            }

            public ContourLinePoint Point1 { get; }
            public ContourLinePoint Point2 { get; }
            public double Elevation { get; }
        }

        private readonly ElevationPoint[][] points;

        /// <summary>
        /// 지도 너비
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// 지도 높이
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// 표고 크기, 지도 상의 최하단과 최상단 등고선의 차이
        /// </summary>
        public double ElevationSize { get; }

        /// <summary>
        /// 가장 서쪽의 좌표값
        /// </summary>
        public double MinX { get; }

        /// <summary>
        /// 가장 동쪽의 좌표값
        /// </summary>
        public double MaxX { get; }

        /// <summary>
        /// 가장 남쪽의 좌표값
        /// </summary>
        public double MinY { get; }

        /// <summary>
        /// 가장 북쪽의 좌표값
        /// </summary>
        public double MaxY { get; }

        /// <summary>
        /// 최하단 등고선 높이
        /// </summary>
        public double MinElevation { get; }

        /// <summary>
        /// 최상단 등고선 높이
        /// </summary>
        public double MaxElevation { get; }

        /// <summary>
        /// 셀 크기
        /// </summary>
        public double CellSize { get; private set; }

        /// <summary>
        /// 표고 테이블
        /// </summary>
        public IReadOnlyList<IReadOnlyList<ElevationPoint>> Elevations { get => points; }
        

        /// <summary>
        /// 좌측 여백
        /// </summary>
        public double CutLeft { get; }
        /// <summary>
        /// 상단 여백
        /// </summary>
        public double CutTop { get; }
        /// <summary>
        /// 우측 여백
        /// </summary>
        public double CutRight { get; }
        /// <summary>
        /// 하단 여백
        /// </summary>
        public double CutBottom { get; }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="lines">등고선 열거</param>
        /// <param name="cellSize">셀 크기</param>
        /// <param name="cutLeft">좌측 여백</param>
        /// <param name="cutTop">상단 여백</param>
        /// <param name="cutRight">우측 여백</param>
        /// <param name="cutBottom">하단 여백</param>
        /// <param name="filterMaskRadius">필터 마스크 반지름</param>
        public ElevationModel(IEnumerable<ContourLine> lines, double cellSize, double cutLeft = double.NaN, double cutTop = double.NaN, double cutRight = double.NaN, double cutBottom = double.NaN, int filterMaskRadius = 1)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));

            MinX = lines.SelectMany(l => l).Min(v => v.X);
            MaxX = lines.SelectMany(l => l).Max(v => v.X);
            MinY = lines.SelectMany(l => l).Min(v => v.Y);
            MaxY = lines.SelectMany(l => l).Max(v => v.Y);

            if (!double.IsNaN(cutLeft)) MinX += Math.Max(0, cutLeft);
            if (!double.IsNaN(cutRight)) MaxX -= Math.Max(0, cutRight);
            if (!double.IsNaN(cutTop)) MinY += Math.Max(0, cutTop);
            if (!double.IsNaN(cutBottom)) MaxY -= Math.Max(0, cutBottom);

            CutLeft = cutLeft;
            CutTop = cutTop;
            CutRight = cutRight;
            CutBottom = cutBottom;

            Width = Math.Max(0, MaxX - MinX);
            Height = Math.Max(0, MaxY - MinY);

            CellSize = cellSize;
            var cellRows = (int)Math.Floor(Height / cellSize) + 1;
            var cellColumns = (int)Math.Floor(Width / cellSize) + 1;

            var cells = new List<ContourLineSegment>[cellRows][];
            for (int i = 0; i < cellRows; i++)
                cells[i] = Enumerable.Range(0, cellColumns).Select(j => new List<ContourLineSegment>()).ToArray();

            if (cellRows <= 0 || cellColumns <= 0)
            {
                points = new ElevationPoint[0][];
                return;
            }

            //등고선에 해당하는 셀을 찾아서 표고 입력
            foreach (var line in lines)
            {
                for (int i = 0; i < line.Count - 1; i++)
                {
                    var start = line[i].X <= line[i + 1].X ? line[i] : line[i + 1];
                    var end = start == line[i + 1] ? line[i] : line[i + 1];

                    start = new ContourLinePoint(start.X - MinX, start.Y - MinY);
                    end = new ContourLinePoint(end.X - MinX, end.Y - MinY);

                    var segment = new ContourLineSegment(start, end, line.Elevation);

                    if (start.X == end.X)
                    {
                        var col = (int)Math.Max(0, Math.Min(cellColumns - 1, Math.Floor(start.X / cellSize)));
                        var row1 = (int)Math.Max(0, Math.Min(cellRows - 1, Math.Floor(Math.Min(start.Y, end.Y) / cellSize)));
                        var row2 = (int)Math.Max(0, Math.Min(cellRows - 1, Math.Floor(Math.Max(start.Y, end.Y) / cellSize)));

                        for (int row = row1; row <= row2; row++)
                            cells[row][col].Add(segment);
                    }
                    else
                    {
                        var col1 = (int)Math.Max(0, Math.Min(cellColumns - 1, Math.Floor(start.X / cellSize)));
                        var col2 = (int)Math.Max(0, Math.Min(cellColumns - 1, Math.Floor(end.X / cellSize)));

                        var a = (end.Y - start.Y) / (end.X - start.X);
                        var b = -start.X * a + start.Y;

                        for (int col = col1; col <= col2; col++)
                        {
                            if (start.Y == end.Y)
                            {
                                var row = (int)Math.Max(0, Math.Min(cellRows - 1, Math.Floor(start.Y / cellSize)));
                                cells[row][col].Add(segment);
                            }
                            else
                            {
                                var x1 = Math.Max(start.X, col * cellSize);
                                var x2 = Math.Min(end.X, (col + 1) * cellSize);

                                var y1 = x1 * a + b;
                                var y2 = x2 * a + b;

                                var row1 = (int)Math.Max(0, Math.Min(cellRows - 1, Math.Floor(Math.Min(y1, y2) / cellSize)));
                                var row2 = (int)Math.Max(0, Math.Min(cellRows - 1, Math.Floor(Math.Max(y1, y2) / cellSize)));

                                for (int row = row1; row <= row2; row++)
                                {
                                    cells[row][col].Add(segment);
                                }
                            }
                        }
                    }
                }
            }


            double pointDistance = cellSize;

            points = new ElevationPoint[cellRows + 1][];
            for (int i = 0; i <= cellRows; i++)
                points[i] = Enumerable.Range(0, cellColumns + 1).Select(j => new ElevationPoint(Math.Min(j * pointDistance, Width), Math.Min(i * pointDistance, Height))).ToArray();



            //행 단위 보간
            double direction = 0;
            for (int i = 0; i <= cellRows; i++)
                CalcElevation(points[i], direction, pointDistance, (x, y) =>
                {
                    var row = (int)(y / cellSize);
                    var column = (int)(x / cellSize);

                    if (column >= cellColumns)
                        return null;

                    return cells[row >= cellRows ? row - 1 : row][column];
                });

            //열 단위 보간
            direction = Math.PI * 0.5;
            for (int j = 0; j <= cellColumns; j++)
                CalcElevation(points.Select(row => row[j]), direction, pointDistance, (x, y) =>
                {
                    var row = (int)(y / cellSize);
                    var column = (int)(x / cellSize);

                    if (row >= cellRows)
                        return null;

                    return cells[row][column >= cellColumns ? column - 1 : column];
                });


            int maxDiagonalWidth = Math.Min(cellRows, cellColumns) + 1;
            int diagonalHeight = cellRows + cellColumns + 1;
            pointDistance = Math.Sqrt(2) * cellSize;

            // 북동-남서 사선 보간
            direction = Math.PI * 0.75;
            for (int i = 0; i < diagonalHeight; i++)
            {
                var diagonalWidth = Math.Min(cellRows <= i ? diagonalHeight - i : i + 1, maxDiagonalWidth);
                CalcElevation(Enumerable.Range(0, diagonalWidth).Reverse().Select(j =>
                {
                    var row = cellRows <= i ? cellRows - j : i - j;
                    var column = cellRows <= i ? i - cellRows + j : j;
                    return points[Math.Max(0, row)][column];
                }).ToArray(), direction, pointDistance, (x, y) =>
                {
                    var row = (int)(y / cellSize);
                    var column = (int)(x / cellSize);

                    return row < cellRows && column > 0 ? cells[row][column - 1] : null;
                });
            }

            // 북서-남동 사선 보간
            direction = Math.PI * 0.25;
            for (int i = 0; i < diagonalHeight; i++)
            {
                var diagonalWidth = Math.Min(cellRows <= i ? diagonalHeight - i : i + 1, maxDiagonalWidth);
                CalcElevation(Enumerable.Range(0, diagonalWidth).Reverse().Select(j =>
                {
                    var row = cellRows <= i ? cellRows - j : i - j;
                    var column = cellColumns - (cellRows <= i ? i - cellRows + j : j);
                    return points[Math.Max(0, row)][column];
                }), direction, pointDistance, (x, y) =>
                {
                    var row = (int)(y / cellSize);
                    var column = (int)(x / cellSize);

                    return row < cellRows && column < cellColumns ? cells[row][column] : null;
                });
            }

            if (double.IsNaN(points[cellRows][cellColumns].Elevation))
            {
                var list = new List<double>
                {
                    points[cellRows - 1][cellColumns - 1].Elevation,
                    points[cellRows][cellColumns - 1].Elevation,
                    points[cellRows - 1][cellColumns].Elevation,
                };

                list.Sort();

                points[cellRows][cellColumns].Elevation = list[list.Count / 2];
            }

            var elevations = points.Select(row => row.Select(point => point.Elevation).ToArray()).ToArray();

            if (filterMaskRadius > 0)
            {
                var filterd = points.Select(row => row.Select(point => point.Elevation).ToArray()).ToArray();

                //중간값 필터 적용
                for (int i = 0; i <= cellRows; i++)
                {
                    for (int j = 0; j <= cellColumns; j++)
                    {
                        int maskX1 = Math.Max(j - filterMaskRadius, 0);
                        int maskX2 = Math.Min(j + filterMaskRadius, cellColumns);
                        int maskY1 = Math.Max(i - filterMaskRadius, 0);
                        int maskY2 = Math.Min(i + filterMaskRadius, cellRows);

                        var list = new List<double>();

                        for (int y = maskY1; y <= maskY2; y++)
                            for (int x = maskX1; x <= maskX2; x++)
                                if (!double.IsNaN(elevations[y][x]))
                                    list.Add(elevations[y][x]);

                        list.Sort();

                        filterd[i][j] = list[list.Count / 2];
                    }
                }

                elevations = filterd;

                //단순 블러 필터 적용
                for (int i = 0; i <= cellRows; i++)
                {
                    for (int j = 0; j <= cellColumns; j++)
                    {
                        int maskX1 = Math.Max(j - filterMaskRadius, 0);
                        int maskX2 = Math.Min(j + filterMaskRadius, cellColumns);
                        int maskY1 = Math.Max(i - filterMaskRadius, 0);
                        int maskY2 = Math.Min(i + filterMaskRadius, cellRows);

                        double weights = 0;
                        double sum = 0;

                        for (int y = maskY1; y <= maskY2; y++)
                            for (int x = maskX1; x <= maskX2; x++)
                            {
                                sum += elevations[y][x];
                                weights += 1;
                            }
                        filterd[i][j] = sum / weights;
                    }
                }

                elevations = filterd;
            }

            for (int i = 0; i <= cellRows; i++)
            {
                for (int j = 0; j <= cellColumns; j++)
                {
                    var point = points[i][j];
                    point.Elevation = elevations[i][j];
                    point.X = MinX;
                    point.Y = MinY;
                }
            }

            MinElevation = points.Min(row => row.Min(p => p.Elevation));
            MaxElevation = points.Max(row => row.Max(p => p.Elevation));
            ElevationSize = MaxElevation - MinElevation;
        }


        static void CalcElevation(IEnumerable<ElevationPoint> points, double direction, double pointDistance, Func<double, double, IEnumerable<ContourLineSegment>> contourLineSelector)
        {
            var queue = new Queue<ElevationPoint>();
            ElevationAround elevationAround = null;
            foreach (var point in points)
            {
                queue.Enqueue(point);
                if (GetElevationArounds(contourLineSelector(point.X, point.Y), point.X, point.Y, direction, pointDistance, out var near, out var far))
                {
                    while (queue.Any())
                    {
                        queue.Dequeue().AddElevationAround(near);
                    }
                    elevationAround = far;
                }
                if (elevationAround != null)
                    point.AddElevationAround(elevationAround);
            }
        }


        private static int Ccw(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            var crossProduct = (x2 - x1) * (y3 - y1) - (x3 - x1) * (y2 - y1);
            return crossProduct > 0 ? 1 : crossProduct < 0 ? -1 : 0;
        }

        private static bool Comparator(double x1, double y1, double x2, double y2) => x1 == x2 ? y1 <= y2 : x1 <= x2;

        private static void Swap(ref double x1, ref double y1, ref double x2, ref double y2)
        {
            double temp;
            temp = x1;
            x1 = x2;
            x2 = temp;
            temp = y1;
            y1 = y2;
            y2 = temp;
        }

        private static bool LineIntersection(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
        {
            int l1_l2 = Ccw(x1, y1, x2, y2, x3, y3) * Ccw(x1, y1, x2, y2, x4, y4);
            int l2_l1 = Ccw(x3, y3, x4, y4, x1, y1) * Ccw(x3, y3, x4, y4, x2, y2);

            if (l1_l2 == 0 && l2_l1 == 0)
            {
                if (Comparator(x2, y2, x1, y1)) Swap(ref x1, ref y1, ref x2, ref y2);
                if (Comparator(x4, y4, x3, y3)) Swap(ref x3, ref y3, ref x4, ref y4);

                return Comparator(x3, y3, x2, y2) && Comparator(x1, y1, x4, y4);
            }
            else
                return l1_l2 <= 0 && l2_l1 <= 0;
        }

        private static bool GetCrossPoint(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4, out double x, out double y)
        {
            if (LineIntersection(x1, y1, x2, y2, x3, y3, x4, y4))
            {
                var denominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
                if (denominator != 0)
                {
                    var a = x1 * y2 - y1 * x2;
                    var b = x3 * y4 - y3 * x4;
                    x = (a * (x3 - x4) - (x1 - x2) * b) / denominator;
                    y = (a * (y3 - y4) - (y1 - y2) * b) / denominator;

                    return true;
                }
            }
            x = 0;
            y = 0;
            return false;
        }

        private static bool GetElevationArounds(IEnumerable<ContourLineSegment> contourLines, double x, double y, double direction, double length, out ElevationAround near, out ElevationAround far)
        {
            if (contourLines != null)
            {
                double x1 = x;
                double y1 = y;
                double x2 = x1 + (direction == Math.PI * 0.5 ? 0 : Math.Cos(direction)) * length;
                double y2 = y1 + Math.Sin(direction) * length;

                var results = contourLines.Select(line =>
                {
                    var x3 = line.Point1.X;
                    var y3 = line.Point1.Y;
                    var x4 = line.Point2.X;
                    var y4 = line.Point2.Y;

                    return GetCrossPoint(x1, y1, x2, y2, x3, y3, x4, y4, out var pointX, out var pointY)
                        ? new ElevationAround(pointX, pointY, line.Elevation) : null;
                }).Where(item => item != null).OrderBy(item => Math.Sqrt(Math.Pow(x1 - item.X, 2) + Math.Pow(y1 - item.Y, 2))).ToArray();

                if (results.Length > 0)
                {
                    near = results.First();
                    far = results.Last();
                    return true;
                }
            }
            near = far = null;
            return false;
        }
    }
}