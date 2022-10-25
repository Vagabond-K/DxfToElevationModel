using DxfToElevationModel.GIS;
using Microsoft.Win32;
using netDxf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VagabondK.Windows;

namespace DxfToElevationModel
{
    class MainViewModel : NotifyPropertyChangeObject
    {
        public ICommand OpenDxfCommand => GetCommand(async () =>
        {
            var openFileDialog = new OpenFileDialog
            {
                DefaultExt = "*.dxf",
                Filter = "DXF File (*.dxf)|*.dxf"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await OpenDxf(openFileDialog.FileName);
            }
        });

        public ElevationModel ElevationModel { get => Get<ElevationModel>(); set => Set(value); }
        public bool MapVisible { get => Get(false); set => Set(value); }
        public bool MapColorInverse { get => Get(false); set => Set(value); }
        public bool IsLoading { get => Get(false); set => Set(value); }

        private Task OpenDxf(string fileName)
            => Task.Run(() =>
            {
                try
                {
                    IsLoading = true;
                    var document = DxfDocument.Load(fileName);
                    ElevationModel = new DxfElevationModel(document, 10);
                    IsLoading = false;
                }
                catch (Exception ex)
                {
                    IsLoading = false;
                    App.Current.Dispatcher.Invoke(() =>
                        ThemeMessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });
    }
}
