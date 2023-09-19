using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SzsaVibeAlgorithm
{
    public class VibeModel
    {
        public Color Color { get; set; }

        public bool IsForegroundPixel { get; set; }

        public VibeModel(Color color, bool isForegroundPixel)
        {
            Color = color;
            IsForegroundPixel = isForegroundPixel;
        }
    }
}
