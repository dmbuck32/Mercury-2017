using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;

namespace Mars_Rover_OCU
{
    public class GMapMarkerRock : GMarkerGoogle
    {
        public int _id;

          public GMapMarkerRock(PointLatLng p, GMarkerGoogleType type, int id)
            : base(p, type)
        {
            this._id = id;
        }

          public int getId()
          {
              return _id;
          }
    }
}
