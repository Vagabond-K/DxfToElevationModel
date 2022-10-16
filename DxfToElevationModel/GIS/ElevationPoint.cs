using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DxfToElevationModel.GIS
{
    /// <summary>
    /// 표고 포인트
    /// </summary>
    public class ElevationPoint
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        public ElevationPoint(double x, double y)
        {
            X = x;
            Y = y;
            elevationAroundList = new List<ElevationAround>();
        }

        /// <summary>
        /// X 좌표
        /// </summary>
        public double X { get; internal set; }
        /// <summary>
        /// Y 좌표
        /// </summary>
        public double Y { get; internal set; }
        private readonly List<ElevationAround> elevationAroundList;
        private double elevation = double.NaN;

        /// <summary>
        /// 주변 표고 추가
        /// </summary>
        /// <param name="elevationAround">주변 표고</param>
        internal void AddElevationAround(ElevationAround elevationAround)
        {
            elevationAroundList.Add(elevationAround);
            elevation = double.NaN;
        }

        /// <summary>
        /// 표고 계산 결과
        /// </summary>
        public double Elevation
        {
            get
            {
                if (double.IsNaN(elevation))
                {
                    if (elevationAroundList.Count == 0)
                        return double.NaN;

                    double sum = 0;
                    double weights = 0;
                    foreach (var item in elevationAroundList)
                    {
                        var distance = Math.Sqrt(Math.Pow(X - item.X, 2) + Math.Pow(Y - item.Y, 2));
                        var weight = (distance == 0) ? 1 : (1 / distance);
                        sum += item.Elevation * weight;
                        weights += weight;
                    }
                    elevation = sum / weights;
                }

                return elevation;
            }
            set
            {
                elevation = value;
            }
        }
    }
}
