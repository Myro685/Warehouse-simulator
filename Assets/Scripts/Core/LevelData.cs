using System;
using System.Collections.Generic;

namespace Warehouse.Core
{
    [Serializable]
    public class LevelData
    {
        public int width;
        public int height;
        public List<TileData> tiles = new List<TileData>();
    }

    [Serializable]
    public struct TileData
    {
        public int x;
        public int y;
        public int type;
    }
}