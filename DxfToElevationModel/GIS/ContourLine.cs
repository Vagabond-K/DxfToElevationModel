using System.Collections.Generic;

namespace DxfToElevationModel.GIS
{
    /// <summary>
    /// 등고선(등고선을 구성하는 포인트의 집합과 표고 정보를 포함)
    /// </summary>
    public class ContourLine : List<ContourLinePoint>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        public ContourLine() : base() { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="collection">등고선 구성 포인트 열거</param>
        public ContourLine(IEnumerable<ContourLinePoint> collection) : base(collection) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="elevation">표고</param>
        public ContourLine(double elevation) : base()
        {
            Elevation = elevation;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="collection">등고선 구성 포인트 열거</param>
        /// <param name="elevation">표고</param>
        public ContourLine(IEnumerable<ContourLinePoint> collection, double elevation) : base(collection)
        {
            Elevation = elevation;
        }

        /// <summary>
        /// 표고
        /// </summary>
        public double Elevation { get; set; }
    }
}
