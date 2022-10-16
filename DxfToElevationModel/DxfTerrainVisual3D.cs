using DxfToElevationModel.GIS;
using netDxf.Entities;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Point = System.Windows.Point;

namespace DxfToElevationModel
{
    /// <summary>
    /// 지형을 표현하는 ModelVisual3D
    /// </summary>
    public class DxfTerrainVisual3D : ModelVisual3D
    {
        /// <summary>
        /// 생성자
        /// </summary>
        public DxfTerrainVisual3D()
        {
            material.Children.Add(textureMaterial);
            material.Children.Add(specularMaterial);
            Content = new GeometryModel3D
            {
                Material = material,
                BackMaterial = backMaterial
            };
        }

        private readonly DiffuseMaterial textureMaterial = new DiffuseMaterial();
        private readonly SpecularMaterial specularMaterial = new SpecularMaterial(new SolidColorBrush(Color.FromArgb(96, 255, 255, 255)), 40);
        private readonly MaterialGroup material = new MaterialGroup();
        private readonly DiffuseMaterial backMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Gray));

        /// <summary>
        /// 모델 업데이트 완료 이벤트
        /// </summary>
        public event EventHandler<EventArgs> ModelUpdated;


        /// <summary>
        /// 지도 표시 여부
        /// </summary>
        public bool MapVisible
        {
            get { return (bool)GetValue(MapVisibleProperty); }
            set { SetValue(MapVisibleProperty, value); }
        }

        /// <summary>
        /// 지도 표시 여부 속성 식별자
        /// </summary>
        public static readonly DependencyProperty MapVisibleProperty =
            DependencyProperty.Register("MapVisible", typeof(bool), typeof(DxfTerrainVisual3D), new PropertyMetadata(false,
                (d, e) => (d as DxfTerrainVisual3D)?.UpdateTexture()));



        /// <summary>
        /// 지도 색상 반전
        /// </summary>
        public bool MapColorInverse
        {
            get { return (bool)GetValue(MapColorInverseProperty); }
            set { SetValue(MapColorInverseProperty, value); }
        }

        /// <summary>
        /// 지도 색상 반전 속성 식별자
        /// </summary>
        public static readonly DependencyProperty MapColorInverseProperty =
            DependencyProperty.Register("MapColorInverse", typeof(bool), typeof(DxfTerrainVisual3D), new PropertyMetadata(false,
                (d, e) => (d as DxfTerrainVisual3D)?.UpdateTexture()));


        /// <summary>
        /// DXF 지도 기반 표고 모델
        /// </summary>
        public DxfElevationModel ElevationModel
        {
            get { return (DxfElevationModel)GetValue(ElevationModelProperty); }
            set { SetValue(ElevationModelProperty, value); }
        }

        /// <summary>
        /// DXF 지도 기반 표고 모델 속성 식별자
        /// </summary>
        public static readonly DependencyProperty ElevationModelProperty =
            DependencyProperty.Register("ElevationModel", typeof(DxfElevationModel), typeof(DxfTerrainVisual3D), new PropertyMetadata(null,
                (d, e) => (d as DxfTerrainVisual3D)?.UpdateModel()));

        private void UpdateModel()
        {
            var elevations = ElevationModel.Elevations;
            if (elevations.Count > 0 && elevations[0].Count > 0)
            {
                int rows = elevations.Count;
                int columns = elevations[0].Count;
                var centerX = ElevationModel.Width / 2;
                var centerY = ElevationModel.Height / 2;
                var width = ElevationModel.Width;
                var height = ElevationModel.Height;
                var cellSize = ElevationModel.CellSize;

                var mesh = new MeshGeometry3D();
                var positions = mesh.Positions;
                var triangleIndices = mesh.TriangleIndices;
                var textureCoordinates = mesh.TextureCoordinates;

                for (var row = 0; row < rows; row++)
                {
                    for (var column = 0; column < columns; column++)
                    {
                        var elevation = elevations[row][column];
                        positions.Add(new Point3D(
                            centerX - Math.Min(column * cellSize, width),
                            Math.Min(row * cellSize, height) - centerY,
                            elevation.Elevation - ElevationModel.MinElevation));
                        textureCoordinates.Add(new Point((double)column / (columns - 1), (double)row / (rows - 1)));
                    }
                }

                for (var row = 0; row < rows - 1; row++)
                {
                    for (var column = 0; column < columns - 1; column++)
                    {
                        int index1 = column + row * columns;
                        int index2 = index1 + columns;

                        triangleIndices.Add(index1);
                        triangleIndices.Add(index2);
                        triangleIndices.Add(index2 + 1);

                        triangleIndices.Add(index1);
                        triangleIndices.Add(index2 + 1);
                        triangleIndices.Add(index1 + 1);
                    }
                }

                (Content as GeometryModel3D).Geometry = mesh;
            }
            else
            {
                Content = null;
            }
            UpdateTexture();
            ModelUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateTexture()
        {
            var elevationModel = ElevationModel;
            if (elevationModel == null)
            {
                textureMaterial.Brush = null;
                return;
            }

            var bitmap = MapVisible
                ? elevationModel.CreateMapTexture(!MapColorInverse)
                : elevationModel.CreateElevationTexture();

            textureMaterial.Brush = bitmap == null
                ? new SolidColorBrush(MapColorInverse ? Colors.White : Colors.Black) as Brush
                : new ImageBrush(System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()));

            specularMaterial.Brush = MapVisible && !MapColorInverse ? new SolidColorBrush(Color.FromArgb(96, 255, 255, 255)) : new SolidColorBrush(Colors.Transparent);
        }
    }
}
