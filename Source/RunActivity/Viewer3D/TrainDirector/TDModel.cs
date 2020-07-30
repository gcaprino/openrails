using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orts.Viewer3D.TrainDirector
{

    public class TDElement
    {
        public int x { get; set; }
        public int y { get; set; }
        public int type { get; set; }
        public int color { get; set; }
        public string shape { get; set; }
        public int dir { get; set; }
        public int switched { get; set; }
    }

    public class TDLayout
    {
        public List<TDElement> layout { get; set; }
    }

    public class TDModel
    {
        public const int TRACK = 1;
        public const int SWITCH = 2;
        public const int SIGNAL = 4;

        public TDLayout Layout;

        public TDModel()
        {
        }

        public TDElement FindElementAt(int x, int y)
        {
            foreach (var el in Layout.layout)
            {
                if (el.x == x && el.y == y)
                    return el;
            }
            return null;
        }
    }
}
