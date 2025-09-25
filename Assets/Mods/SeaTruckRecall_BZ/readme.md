# Sea Truck Recall BZ

This mod replaces the "Nothing Docked" console UI on the Seatruck Dock introduced in the "What the Dock" update to Below Zero. The new UI allows the player to automatically recall the closest Seatruck to the dock. The Seatruck will automatically pilot itself, avoiding terrain and obstacles, and will navigate all the way into the docking tube to dock.

![](https://raw.githubusercontent.com/mroshaw/BelowZeroThunderKitMods/master/Assets/Mods/SeaTruckRecall_BZ/media~/recallui.png)

## Implementation details

The navigation system works on the basis of a `NavGrid`. This is a three dimensional cube as an array of `NavCells` - essentially spheres at intersecting points throughout the `NavGrid`. Each cell has a Boolean value that determines whether or not there is an obstacle/collider within the bounds of that cell, which would block the Seatruck from travelling through it.

This 3D grid is generated with the Seatruck Dock in it's center and each cell is evaluated as to whether it's "walkable" by means of a sphere cast that detects colliders on a particular set of layers.

![](https://raw.githubusercontent.com/mroshaw/BelowZeroThunderKitMods/master/Assets/Mods/SeaTruckRecall_BZ/media~/navgridexample.png)

Hopefully you can see in the screenshot above, the grid cube with it's center at the little chequered flag sphere that represents the end of the docking tube. Green cells are traversable, red cells are not.

The size and density of the grid is determined by the range (how far out from the center point the last cell sits on a particular axis) and the number of cell extents (that is, how many cells are created and evaluated in each axis from the center of the grid). A large range and small cell number gives good performance but poor accuracy. A small range and large cell number gives better accuracy but is more processor intensive - each cell must be evaluated as "walkable", so any combination of the two parameters that increase the number of cells means more calculations. Both parameters are configurable in the mod options.

The total number of cells generated and evaluated can be calculated as: `(cellExtents + 1)^3` and the distance between each cell as: `maxRange\(cellExtents+1)`

A navigation agent, in this case our Seatruck, can use this grid to calculate a path from it's current location within the grid to the dock. It does this by traversing the grid, looking for a way to get from cell to cell without being blocked by an unpassable cell.

![](https://raw.githubusercontent.com/mroshaw/BelowZeroThunderKitMods/master/Assets/Mods/SeaTruckRecall_BZ/media~/pathexample.png)

In this screenshot, you can just make out the white spheres that represent the path from the Seatruck's original position to the end of the docking tube. This path is called pathfinding, and is implemented using the [A* path finding algorithm](https://en.wikipedia.org/wiki/A*_search_algorithm).

## Limitations / Considerations

A `NavGrid` is generated when the game is started / loaded for each Seatruck Dock that's spawned. A new grid will be created if and when a new Dock is built by the player. This means the validity of each cell is correct at the point in time the grid was created - if the player were to spawn a load of obstacles in the game, the grid won't know about them. There is a button in the mod options to regenerate all grids, but it's not ideal.

The grid doesn't particularly take into account the size or makeup of the selected Seatruck. So you may have many modules attached, aquariums and suchlike, that make even "valid" cells unpassable.

And lastly, the grid itself takes processing time and memory to create and refresh. Though this is done asynchronously, via `Coroutines`, it still can have an impact on game performance. Path calculation is also processor intensive, especially where large grids and/or shorter cell distances are involved. I'm actively tuning both algorithms to try to get a balance of performance and accuracy.

## Attribution / Credits

