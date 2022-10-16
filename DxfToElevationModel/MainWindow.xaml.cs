using System;
using VagabondK.Windows;

namespace DxfToElevationModel
{
    public partial class MainWindow : ThemeWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel();
        }

        private void DxfTerrainVisual3D_ModelUpdated(object sender, EventArgs e)
        {
            viewPort.FitView(viewPort.Camera.LookDirection, viewPort.Camera.UpDirection);
        }
    }
}
