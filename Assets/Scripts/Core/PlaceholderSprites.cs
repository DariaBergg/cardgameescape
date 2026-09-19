using System.Collections.Generic;
using UnityEngine;

public static class PlaceholderSprites
{
    static readonly Dictionary<Color, Sprite> cache = new Dictionary<Color, Sprite>();

    public static Sprite Square(Color color)
    {
        if (cache.TryGetValue(color, out var cached) && cached != null) return cached;

        const int size = 64;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        cache[color] = sprite;
        return sprite;
    }
}
