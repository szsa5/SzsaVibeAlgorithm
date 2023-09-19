using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SzsaVibeAlgorithm
{
    public class BinaryPixel
    {

        public int FrameId { get;}

        public bool IsForeground { get; }

        public BinaryPixel(int frameId, bool isForeground)
        {
            FrameId = frameId;
            IsForeground = isForeground;
        }
    }
}
