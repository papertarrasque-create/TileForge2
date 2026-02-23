using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Data;

namespace TileForge.Export;

public static class PngExporter
{
    public static void Export(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch,
                              MapData map, List<TileGroup> groups,
                              Dictionary<string, TileGroup> groupsByName,
                              ISpriteSheet sheet, string outputPath)
    {
        int pixelW = map.Width * sheet.TileWidth;
        int pixelH = map.Height * sheet.TileHeight;

        using var renderTarget = new RenderTarget2D(graphicsDevice, pixelW, pixelH);
        graphicsDevice.SetRenderTarget(renderTarget);
        graphicsDevice.Clear(Color.Transparent);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        for (int layerIdx = 0; layerIdx < map.Layers.Count; layerIdx++)
        {
            var layer = map.Layers[layerIdx];
            if (!layer.Visible) continue;

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    string groupName = layer.GetCell(x, y, map.Width);
                    if (groupName == null) continue;
                    if (!groupsByName.TryGetValue(groupName, out var group)) continue;
                    if (group.Sprites.Count == 0) continue;

                    // Same position-seeded variation as MapCanvas
                    int spriteIdx = group.Sprites.Count == 1
                        ? 0
                        : ((x * 31 + y * 37) % group.Sprites.Count + group.Sprites.Count) % group.Sprites.Count;

                    var sprite = group.Sprites[spriteIdx];
                    var srcRect = sheet.GetTileRect(sprite.Col, sprite.Row);
                    var destRect = new Rectangle(x * sheet.TileWidth, y * sheet.TileHeight,
                                                 sheet.TileWidth, sheet.TileHeight);

                    spriteBatch.Draw(sheet.Texture, destRect, srcRect, Color.White);
                }
            }

            // Draw entities after designated layer
            if (layerIdx == map.EntityRenderOrder)
            {
                DrawEntities(spriteBatch, map, groupsByName, sheet);
            }
        }

        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);

        using var stream = File.Create(outputPath);
        renderTarget.SaveAsPng(stream, pixelW, pixelH);
    }

    private static void DrawEntities(SpriteBatch spriteBatch, MapData map,
                                      Dictionary<string, TileGroup> groupsByName,
                                      ISpriteSheet sheet)
    {
        foreach (var entity in map.Entities)
        {
            if (!groupsByName.TryGetValue(entity.GroupName, out var group)) continue;
            if (group.Sprites.Count == 0) continue;

            var sprite = group.Sprites[0];
            var srcRect = sheet.GetTileRect(sprite.Col, sprite.Row);
            var destRect = new Rectangle(entity.X * sheet.TileWidth, entity.Y * sheet.TileHeight,
                                         sheet.TileWidth, sheet.TileHeight);
            spriteBatch.Draw(sheet.Texture, destRect, srcRect, Color.White);
        }
    }
}
