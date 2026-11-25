using UnityEngine;
using UnityEngine.UI;

namespace Gilzoide.GradientRect
{
    [AddComponentMenu("UI/Effects/Gradient Image Effect")]
    [RequireComponent(typeof(Image))]
    public class GradientImageEffect : GradientEffect
    {
        protected override Rect GetGradientRect(VertexHelper mesh)
        {
            Rect rect = base.GetGradientRect(mesh);
            if (!(graphic is Image image))
                return rect;

            Sprite sprite = image.overrideSprite;
            if (!image.preserveAspect || sprite == null ||
                (image.type != Image.Type.Simple && image.type != Image.Type.Filled))
                return rect;

            if (image.type == Image.Type.Simple && image.useSpriteMesh)
            {
                // A tight sprite mesh can have different drawing bounds from its quad.
                var vertex = new UIVertex();
                mesh.PopulateUIVertex(ref vertex, 0);
                Vector2 min = vertex.position;
                Vector2 max = min;
                for (int i = 1; i < mesh.currentVertCount; i++)
                {
                    mesh.PopulateUIVertex(ref vertex, i);
                    min = Vector2.Min(min, vertex.position);
                    max = Vector2.Max(max, vertex.position);
                }

                return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }

            Vector2 spriteSize = sprite.rect.size;
            int width = Mathf.RoundToInt(spriteSize.x);
            int height = Mathf.RoundToInt(spriteSize.y);
            if (width <= 0 || height <= 0)
                return rect;

            // Reconstruct the full drawing rect so changing fill amount only clips the gradient.
            float spriteAspect = spriteSize.x / spriteSize.y;
            Vector2 fittedSize = new Vector2(
                Mathf.Min(rect.width, rect.height * spriteAspect),
                Mathf.Min(rect.height, rect.width / spriteAspect));
            Vector2 fittedMin = rect.min + Vector2.Scale(rect.size - fittedSize, image.rectTransform.pivot);
            Vector4 padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);
            return Rect.MinMaxRect(
                fittedMin.x + fittedSize.x * (padding.x / width),
                fittedMin.y + fittedSize.y * (padding.y / height),
                fittedMin.x + fittedSize.x * ((width - padding.z) / width),
                fittedMin.y + fittedSize.y * ((height - padding.w) / height));
        }
    }
}
