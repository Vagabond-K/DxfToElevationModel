using System;
using System.Collections.Generic;
using System.Text;

namespace DxfToElevationModel.GIS
{
    class ElevationAround
    {
        public ElevationAround(double x, double y, double elevation)
        {
            X = x;
            Y = y;
            Elevation = elevation;
        }

        public double X { get; }
        public double Y { get; }
        public double Elevation { get; }
    }
}
