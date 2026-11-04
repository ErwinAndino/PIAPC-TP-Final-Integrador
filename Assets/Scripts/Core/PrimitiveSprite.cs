using UnityEngine;

namespace DefendAndCollect.Core
{
    public static class PrimitiveSprite
    {
        private static Sprite sharedSprite;

        public static Sprite Get()
        {
            if (sharedSprite != null)
            {
                return sharedSprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            sharedSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            sharedSprite.name = "PrimitiveSprite";

            return sharedSprite;
        }
    }
}
