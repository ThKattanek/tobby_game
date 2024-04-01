﻿using SFML.Graphics;
using SFML.System;
using sfmlgame.Assets;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace sfmlgame.World
{
    public class Chunk
    {
        public bool IsActive { get; private set; }
        public Vector2i Position { get; private set; } // Position in the chunk grid
        private List<WorldTile> tiles = new List<WorldTile>(); // List of tiles in the chunk
        private int tileSize;
        private int chunkSize = 32; // Number of tiles per side in the chunk

        public Chunk(Vector2i position, int tileSize)
        {
            this.tileSize = tileSize;
            Reset(position, null); // Null passed initially for texture, assuming tiles are set later
            indexText = new Text($"({Position.X}, {Position.Y})", GameAssets.Instance.pixelFont1, 19);

            debugOutline = new RectangleShape(new Vector2f(chunkSize * tileSize, chunkSize * tileSize));
        }

        // Activates or deactivates the chunk
        public void SetActive(bool active)
        {
            IsActive = active;
        }

        // Resets the chunk for reuse, including setting its new position and reinitializing its tiles
        public void Reset(Vector2i newPosition, Sprite tileTexture)
        {
            Position = newPosition;
            tiles.Clear();

            double scale = 0.1; // Adjust for more or less frequent terrain changes
            double z = 0; // Keep constant if you want a 2D noise

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {

                    double noiseValue = WorldManager.perlin.Noise((Position.X * chunkSize + x) * scale, (Position.Y * chunkSize + y) * scale, z);
                    TileType type = DetermineTileTypeBasedOnNoise(noiseValue);
                    Vector2f tilePosition = new Vector2f(Position.X * chunkSize * tileSize + x * tileSize, Position.Y * chunkSize * tileSize + y * tileSize);

                    // Get the corresponding sprite for the determined tile type
                    Sprite sprite = GameAssets.Instance.GetTileSprite(type);
                    var newTile = new WorldTile(sprite, tilePosition, type);
                    newTile = AttachObject(newTile, type);
                    tiles.Add(newTile);

                    //if (type == TileType.TreeObject)
                    //{
                    //    Console.WriteLine($"Tree tile at: {tilePosition} in chunk {Position}");
                    //}
                }
            }

            AddWaterBodies();

            AddTreeBodies();

        }

        public const int MaxWaterSeedsPerChunk = 20;

        public void AddWaterBodies()
        {
            double scale = 0.2; // Consider experimenting with this value
            double threshold = 0.1; // Adjust as necessary
            int maxRadius = 5;
            Random rnd = new Random();
            HashSet<Vector2i> waterSeeds = new HashSet<Vector2i>();

            for (int i = 0; i < MaxWaterSeedsPerChunk; i++)
            {
                int x = rnd.Next(0, chunkSize);
                int y = rnd.Next(0, chunkSize);
                int worldX = Position.X * chunkSize + x;
                int worldY = Position.Y * chunkSize + y;

                double noiseValue = WorldManager.perlin.Noise(worldX * scale, worldY * scale, 0);
                if (noiseValue > threshold)
                {
                    int radius = rnd.Next(2, maxRadius);
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            if (dx * dx + dy * dy <= radius * radius)
                            {
                                int targetX = x + dx;
                                int targetY = y + dy;
                                if (targetX >= 0 && targetX < chunkSize && targetY >= 0 && targetY < chunkSize)
                                {
                                    SetTileTypeAtPosition(targetX, targetY, TileType.WaterTile);
                                }
                            }
                        }
                    }
                    waterSeeds.Add(new Vector2i(worldX, worldY));
                }
            }
        }

        public const int MaxTreeSeedsPerChunk = 50; // Lower the maximum number of tree seeds per chunk

        public void AddTreeBodies()
        {
            double scale = 0.2; // Adjust the scale for tree noise
            double treeThreshold = 0.09; // Increase the threshold for less frequent trees
            Random rnd = new Random();
            HashSet<Vector2i> treeSeeds = new HashSet<Vector2i>();

            // Determine number of trees based on the chunk's Perlin noise value
            int numberOfTrees = rnd.Next(1, MaxTreeSeedsPerChunk + 1); // Use random number for seeds count

            for (int i = 0; i < numberOfTrees; i++)
            {
                int x = rnd.Next(0, chunkSize);
                int y = rnd.Next(0, chunkSize);
                int worldX = Position.X * chunkSize + x;
                int worldY = Position.Y * chunkSize + y;

                double noiseValue = WorldManager.perlin.Noise(worldX * scale, worldY * scale, 0);
                if (noiseValue > treeThreshold)
                {
                    // Decide if it's a cluster or a single tree
                    bool isCluster = rnd.NextDouble() > 0.3; // Adjust probability of a cluster

                    if (isCluster)
                    {
                        int clusterRadius = rnd.Next(1, 3); // Smaller radius for tree clusters
                        for (int dx = -clusterRadius; dx <= clusterRadius; dx++)
                        {
                            for (int dy = -clusterRadius; dy <= clusterRadius; dy++)
                            {
                                if (Math.Sqrt(dx * dx + dy * dy) <= clusterRadius)
                                {
                                    int targetX = x + dx;
                                    int targetY = y + dy;
                                    if (targetX >= 0 && targetX < chunkSize && targetY >= 0 && targetY < chunkSize)
                                    {
                                        SetTileTypeAtPosition(targetX, targetY, TileType.TreeObject);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Single tree
                        SetTileTypeAtPosition(x, y, TileType.TreeObject);
                    }
                    treeSeeds.Add(new Vector2i(worldX, worldY));
                }
            }
        }

        private void SetTileTypeAtPosition(int targetX, int targetY, TileType tileType)
        {
            Vector2f worldPosition = new Vector2f(Position.X * chunkSize * tileSize + targetX * tileSize,
                                                   Position.Y * chunkSize * tileSize + targetY * tileSize);

            // Find the base tile at the specified position
            var baseTile = tiles.FirstOrDefault(tile => Math.Abs(tile.Sprite.Position.X - worldPosition.X) < float.Epsilon &&
                                                         Math.Abs(tile.Sprite.Position.Y - worldPosition.Y) < float.Epsilon);

            // If there's no tile at this position, we create a new one which should be grass or water
            if (baseTile == null)
            {
                // First, create a grass base tile
                Sprite grassSprite = GameAssets.Instance.GetTileSprite(TileType.Grass);
                baseTile = new WorldTile(grassSprite, worldPosition, tileType) { Type = TileType.Grass };
                tiles.Add(baseTile);
            }

            if (tileType == TileType.TreeObject)
            {
                baseTile = AttachObject(baseTile, TileType.TreeObject);
            }

            if (tileType == TileType.Rock)
            {
                baseTile = AttachObject(baseTile, TileType.Rock);
            }

            // If we're setting a water tile, replace the base tile entirely
            else if (tileType == TileType.WaterTile)
            {
                tiles.Remove(baseTile);
                Sprite waterSprite = GameAssets.Instance.GetTileSprite(TileType.WaterTile);
                baseTile = new WorldTile(waterSprite, worldPosition, tileType) { Type = TileType.WaterTile };
                tiles.Add(baseTile);
            }
        }

        private WorldTile AttachObject(WorldTile tile, TileType type)
        {
            Sprite objectSprite = GameAssets.Instance.GetTileSprite(type);
            tile.Object = new WorldTile(objectSprite, tile.Sprite.Position, TileType.TreeObject);
            return tile;
        }




        private TileType DetermineTileTypeBasedOnNoise(double baseNoise)
        {

            // Fall back to non-tree tiles
            if (baseNoise < 0.3) return TileType.Grass;
            else if (baseNoise < 0.7) return TileType.Rock;
            else return TileType.Grass; // Fallback to grass for higher values, which were rocks previously
        }



        // Updates the chunk, if there's any dynamic content or logic to process
        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            // Update logic for tiles or entities within the chunk
        }

        // Draws the chunk's content to the given render target
        public void Draw(RenderTexture target)
        {
            if (IsActive)
            {

                foreach (var tile in tiles)
                {
                    tile.Draw(target);
                }


            }
            if(Game.Instance.Debug)
                DrawDebug(target);
        }

        RectangleShape debugOutline;
        Text indexText;

        public void DrawDebug(RenderTexture target)
        {
            if (!IsActive) return;

            // Update positions first before drawing
            debugOutline.Position = new Vector2f(Position.X * chunkSize * tileSize, Position.Y * chunkSize * tileSize);
            indexText.Position = new Vector2f(Position.X * chunkSize * tileSize, Position.Y * chunkSize * tileSize);
            indexText.DisplayedString = $"({Position.X}, {Position.Y})"; // Ensure the text is updated every draw call

            // Set styles (this could be moved to the constructor or Reset method to avoid setting it every frame)
            debugOutline.OutlineThickness = 1f;
            debugOutline.OutlineColor = Color.Red;
            debugOutline.FillColor = Color.Transparent;
            indexText.Color = Color.Black;
            indexText.OutlineColor = Color.White;
            indexText.OutlineThickness = 1;

            // Now, draw the debug elements
            target.Draw(debugOutline);
            target.Draw(indexText);

            //Console.WriteLine($"Drawing debug for chunk at position: {Position}");
        }

        public bool ContainsPosition(Vector2f position)
        {
            float left = Position.X * chunkSize * tileSize;
            float top = Position.Y * chunkSize * tileSize;
            float right = left + chunkSize * tileSize;
            float bottom = top + chunkSize * tileSize;

            return position.X >= left && position.X < right && position.Y >= top && position.Y < bottom;
        }

    }
}
