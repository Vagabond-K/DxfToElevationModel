using DxfToElevationModel.GIS;
using netDxf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxfToElevationModel
{
    /// <summary>
    /// DXF 지도 기반 표고 모델
    /// </summary>
    public class DxfElevationModel : ElevationModel
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="document">DXF 문서</param>
        /// <param name="cellSize">셀 크기</param>
        /// <param name="cutLeft">좌측 여백</param>
        /// <param name="cutTop">상단 여백</param>
        /// <param name="cutRight">우측 여백</param>
        /// <param name="cutBottom">하단 여백</param>
        /// <param name="filterMaskRadius">필터 마스크 반지름</param>
        public DxfElevationModel(DxfDocument document, double cellSize, double cutLeft = double.NaN, double cutTop = double.NaN, double cutRight = double.NaN, double cutBottom = double.NaN, int filterMaskRadius = 1)
            : base(document?.LwPolylines?.Where(line => line.Elevation != 0)?.Select(
                line => new ContourLine(line.Vertexes.Select(
                    point => new ContourLinePoint(point.Position.X, -point.Position.Y)), line.Elevation))?.ToArray() ?? throw new ArgumentNullException(nameof(document)), cellSize, cutLeft, cutTop, cutRight, cutBottom, filterMaskRadius)
        {
            Document = document;
        }

        /// <summary>
        /// DXF 문서
        /// </summary>
        public DxfDocument Document { get; }

        /// <summary>
        /// 표고 컬러맵 텍스쳐 생성
        /// </summary>
        /// <param name="elevationColors">단계별 표고 색상</param>
        /// <returns>표고 컬러맵 텍스쳐</returns>
        public Bitmap CreateElevationTexture(params Color[] elevationColors)
        {
            if (elevationColors == null || elevationColors.Length == 0)
                elevationColors = new Color[] { Color.Blue, Color.Cyan, Color.Lime, Color.Yellow, Color.Red, Color.White };

            var minElevation = MinElevation;
            var elevationSize = ElevationSize;
            var elevations = Elevations;

            var elevationBitmap = new Bitmap(elevations[0].Count * 2 - 2, elevations.Count * 2 - 2);
            var bitmapData = elevationBitmap.LockBits(new Rectangle(0, 0, elevationBitmap.Width, elevationBitmap.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, elevationBitmap.PixelFormat);
            IntPtr ptr = bitmapData.Scan0;
            int bytes = Math.Abs(bitmapData.Stride) * elevationBitmap.Height;
            byte[] rgbValues = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int i = 0; i < elevations.Count - 1; i++)
                for (int j = 0; j < elevations[0].Count - 1; j++)
                {
                    var color = GetElevationColor((elevations[i][j].Elevation - minElevation) / elevationSize, elevationColors);

                    rgbValues[i * 2 * 4 * elevationBitmap.Width + j * 2 * 4] = color.B;
                    rgbValues[i * 2 * 4 * elevationBitmap.Width + j * 2 * 4 + 1] = color.G;
                    rgbValues[i * 2 * 4 * elevationBitmap.Width + j * 2 * 4 + 2] = color.R;
                    rgbValues[i * 2 * 4 * elevationBitmap.Width + j * 2 * 4 + 3] = color.A;

                    if (j > 0)
                    {
                        rgbValues[i * 2 * 4 * elevationBitmap.Width + (j * 2 - 1) * 4] = color.B;
                        rgbValues[i * 2 * 4 * elevationBitmap.Width + (j * 2 - 1) * 4 + 1] = color.G;
                        rgbValues[i * 2 * 4 * elevationBitmap.Width + (j * 2 - 1) * 4 + 2] = color.R;
                        rgbValues[i * 2 * 4 * elevationBitmap.Width + (j * 2 - 1) * 4 + 3] = color.A;
                    }

                    if (i > 0)
                    {
                        rgbValues[(i * 2 - 1) * 4 * elevationBitmap.Width + j * 2 * 4] = color.B;
                        rgbValues[(i * 2 - 1) * 4 * elevationBitmap.Width + j * 2 * 4 + 1] = color.G;
                        rgbValues[(i * 2 - 1) * 4 * elevationBitmap.Width + j * 2 * 4 + 2] = color.R;
                        rgbValues[(i * 2 - 1) * 4 * elevationBitmap.Width + j * 2 * 4 + 3] = color.A;
                    }

                    if (i > 0 && j > 0)
                    {
                        rgbValues[(i * 2 - 1) * 4 * elevationBitmap.Width + (j * 2 - 1) * 4] = color.B;
                        rgbValues[(i * 2 - 1) * 4 * elevationBitmap.Width + (j * 2 - 1) * 4 + 1] = color.G;
                        rgbValues[(i * 2 - 1) * 4 * elevationBitmap.Width + (j * 2 - 1) * 4 + 2] = color.R;
                        rgbValues[(i * 2 - 1) * 4 * elevationBitmap.Width + (j * 2 - 1) * 4 + 3] = color.A;
                    }
                }

            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            elevationBitmap.UnlockBits(bitmapData);

            return elevationBitmap;
        }

        /// <summary>
        /// 지도 텍스쳐 색성
        /// </summary>
        /// <param name="isDarkTheme">다크 테마 여부</param>
        /// <param name="scale">스케일</param>
        /// <returns>지도 텍스쳐</returns>
        public Bitmap CreateMapTexture(bool isDarkTheme = false, float scale = 1f)
        {
            var document = Document;

            if (Elevations.Count > 0)
            {
                double minX = MinX;
                double minY = MinY;

                //지도 비트맵 이미지 생성
                var bitmap = new Bitmap((int)Math.Floor((Elevations[0].Count - 1) * CellSize * scale), (int)Math.Floor((Elevations.Count - 1) * CellSize * scale));
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.FillRectangle(new SolidBrush(isDarkTheme ? Color.Black : Color.White), 0, 0, bitmap.Width, bitmap.Height);

                    //LwPolyline 기반 지도 구성요소 그리기
                    foreach (var item in document.LwPolylines)
                    {
                        //캐드에 사용된 색상 추출
                        var cadColor = item.Color.IsByLayer ? item.Layer.Color : item.Color;
                        var color = Color.FromArgb(255, cadColor.R, cadColor.G, cadColor.B);
                        if (!isDarkTheme) color = InvertLightness(color);
                        var pen = new Pen(color) { Width = scale };

                        graphics.DrawLines(pen,
                            item.Vertexes.Select(v => new PointF((float)(v.Position.X - minX) * scale,
                            (float)(-v.Position.Y - minY) * scale)).ToArray());
                    }

                    //텍스트 그리기
                    foreach (var item in document.Texts)
                    {
                        var defaultFont = new Font(System.Windows.SystemFonts.CaptionFontFamily.FamilyNames.First().Value, (float)item.Height * scale);
                        var cadColor = item.Color.IsByLayer ? item.Layer.Color : item.Color;
                        var color = Color.FromArgb(255, cadColor.R, cadColor.G, cadColor.B);
                        if (!isDarkTheme) color = InvertLightness(color);
                        var textBrush = new SolidBrush(color);

                        graphics.DrawString(item.Value, defaultFont, textBrush,
                            (float)(item.Position.X - minX) * scale,
                            (float)(-item.Position.Y - minY) * scale);
                    }
                }
                return bitmap;
            }

            return null;
        }

        private static Color GetElevationColor(double elevationRatio, Color[] elevationColors)
        {
            if (elevationColors.Length == 0)
                return Color.Black;
            if (elevationColors.Length == 1)
                return elevationColors[0];
            if (elevationRatio == 1)
                return elevationColors[elevationColors.Length - 1];

            var levels = elevationColors.Length - 1;
            var index = (int)Math.Floor(elevationRatio * levels);
            var firstColor = elevationColors[index];
            var secondColor = elevationColors[index + 1];
            var gradient = elevationRatio * levels - index;
            var color = Color.FromArgb(
                (byte)Math.Round(firstColor.A * (1 - gradient) + secondColor.A * gradient),
                (byte)Math.Round(firstColor.R * (1 - gradient) + secondColor.R * gradient),
                (byte)Math.Round(firstColor.G * (1 - gradient) + secondColor.G * gradient),
                (byte)Math.Round(firstColor.B * (1 - gradient) + secondColor.B * gradient));

            return color;
        }

        /// <summary>
        /// CIE Lab 색공간 기반 밝기 반전
        /// </summary>
        /// <param name="color">색상</param>
        /// <returns>밝기가 반전된 색상</returns>
        private static Color InvertLightness(Color color)
        {
            double R = color.R;
            double G = color.G;
            double B = color.B;

            R = R > 10.31475 ? Math.Pow(R / 269.025 + 0.0521327014218009, 2.4) * 100 : R / 32.946;
            G = G > 10.31475 ? Math.Pow(G / 269.025 + 0.0521327014218009, 2.4) * 100 : G / 32.946;
            B = B > 10.31475 ? Math.Pow(B / 269.025 + 0.0521327014218009, 2.4) * 100 : B / 32.946;

            double x = R * 0.0043389060149189 + G * 0.0037623491535767 + B * 0.0018990604648227;
            double y = R * 0.002126 + G * 0.007152 + B * 0.000722;
            double z = R * 0.0001772544841710827 + G * 0.0010947530835851 + B * 0.0087295537411717;

            x = x > 0.0088564516790356 ? Math.Pow(x, 0.3333333333333333) : x * 7.787037037037037 + 0.1379310344827586;
            y = y > 0.0088564516790356 ? Math.Pow(y, 0.3333333333333333) : y * 7.787037037037037 + 0.1379310344827586;
            z = z > 0.0088564516790356 ? Math.Pow(z, 0.3333333333333333) : z * 7.787037037037037 + 0.1379310344827586;

            double L = Math.Max(0, 116 * y - 16);
            double a = 500 * (x - y);
            double b = 200 * (y - z);


            //밝기를 반전시키키 위해서 RGB 색상을 Lab 색상으로 변환하여 L을 반전시킨 후 다시 RGB 색상으로 변환
            L = 100 - L;


            y = L / 116 + 0.1379310344827586;
            x = a / 500 + y;
            z = y - b / 200;

            x = x > 0.2068965517241379 ? Math.Pow(x, 3) : x / 7.787 - 0.0177129876053369;
            y = y > 0.2068965517241379 ? Math.Pow(y, 3) : y / 7.787 - 0.0177129876053369;
            z = z > 0.2068965517241379 ? Math.Pow(z, 3) : z / 7.787 - 0.0177129876053369;

            // Observer = 2°, Illuminant = D65
            R = 3.080093082 * x - 1.5372 * y - 0.542890638 * z;
            G = -0.920910383 * x + 1.8758 * y + 0.045186445 * z;
            B = 0.052941179 * x - 0.2040 * y + 1.150893310 * z;

            R = R > 0.0031308 ? Math.Pow(R, 0.4166666666666667) * 269.025 - 14.025 : R * 3294.6;
            G = G > 0.0031308 ? Math.Pow(G, 0.4166666666666667) * 269.025 - 14.025 : G * 3294.6;
            B = B > 0.0031308 ? Math.Pow(B, 0.4166666666666667) * 269.025 - 14.025 : B * 3294.6;

            R = Math.Max(Math.Min(R, 255), 0);
            G = Math.Max(Math.Min(G, 255), 0);
            B = Math.Max(Math.Min(B, 255), 0);

            return Color.FromArgb((int)Math.Round(R), (int)Math.Round(G), (int)Math.Round(B));
        }
    }
}
