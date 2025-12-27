namespace Warehouse.Core
{
    /// <summary>
    /// Definuje typ objektu na dané souřadnici mřížky.
    /// Používá se pro logiku vykreslování i pathfindingu.
    /// </summary>
    
    public enum TileType
    {
        Empty,
        Wall,
        Shelf,
        LoadingDock,
        UnloadingDock
    }
}