namespace DxfToElevationModel.GIS
{
    /// <summary>
    /// 등고선 구성 포인트
    /// </summary>
    public struct ContourLinePoint
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        public ContourLinePoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// X 좌표
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Y 좌표
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// == 연산자 정의
        /// </summary>
        /// <param name="point1">등고선 구성 포인트 1</param>
        /// <param name="point2">등고선 구성 포인트 2</param>
        /// <returns>값 일치 여부</returns>
        public static bool operator ==(ContourLinePoint point1, ContourLinePoint point2) => point1.Equals(point2);

        /// <summary>
        /// != 연산자 정의
        /// </summary>
        /// <param name="point1">등고선 구성 포인트 1</param>
        /// <param name="point2">등고선 구성 포인트 2</param>
        /// <returns>값 불일치 여부</returns>
        public static bool operator !=(ContourLinePoint point1, ContourLinePoint point2) => !point1.Equals(point2);

        /// <summary>
        /// 이 인스턴스와 지정된 개체가 같은지 여부를 나타냅니다.
        /// </summary>
        /// <param name="obj">현재 인스턴스와 비교할 개체입니다.</param>
        /// <returns>일치 여부</returns>
        public override bool Equals(object obj) => base.Equals(obj);

        /// <summary>
        /// 이 인스턴스의 해시 코드를 반환합니다.
        /// </summary>
        /// <returns>이 인스턴스의 해시 코드인 32비트 부호 있는 정수입니다.</returns>
        public override int GetHashCode() => base.GetHashCode();
    }
}
