using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Gilzoide.GradientRect
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class GradientImage : GradientRect
    {
        [SerializeField] protected Sprite _sprite;

        [SerializeField] private Image.Type _type = Image.Type.Simple;
        [SerializeField] private bool _fillCenter = true;
        [SerializeField] private float _pixelsPerUnitMultiplier = 1f;

        private static readonly Vector2[] s_VertScratch = new Vector2[4];
        private static readonly Vector2[] s_UVScratch = new Vector2[4];

        public float PixelsPerUnitMultiplier
        {
            get => _pixelsPerUnitMultiplier;
            set
            {
                _pixelsPerUnitMultiplier = Mathf.Max(0.01f, value);
                SetVerticesDirty();
            }
        }

        public Image.Type Type
        {
            get => _type;
            set
            {
                if (_type == value)
                    return;

                _type = value;
                SetVerticesDirty();
            }
        }

        public bool FillCenter
        {
            get => _fillCenter;
            set
            {
                if (_fillCenter == value)
                    return;

                _fillCenter = value;
                SetVerticesDirty();
            }
        }

        public bool HasBorder
        {
            get
            {
                if (_sprite == null)
                    return false;

                Vector4 v = _sprite.border;
                return v.sqrMagnitude > 0f;
            }
        }

        public Sprite Sprite
        {
            get => _sprite;
            set
            {
                if (_sprite == value)
                    return;

                _sprite = value;
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        public override Texture mainTexture
        {
            get
            {
                if (_sprite != null)
                    return _sprite.texture;

                if (material != null && material.mainTexture != null)
                    return material.mainTexture;

                return s_WhiteTexture;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            switch (_type)
            {
                case Image.Type.Simple:
                    base.OnPopulateMesh(vh);
                    break;
                case Image.Type.Sliced:
                    GenerateSlicedSprite(vh);
                    break;
                default:
                    vh.Clear();
                    Debug.LogWarning($"Unsupported image type: {_type}");
                    break;
            }
        }

        private void GenerateSlicedSprite(VertexHelper vh)
        {
            if (!HasBorder)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            Vector4 outer, inner, padding, border;

            if (_sprite != null)
            {
                outer = DataUtility.GetOuterUV(_sprite);
                inner = DataUtility.GetInnerUV(_sprite);
                padding = DataUtility.GetPadding(_sprite);
                border = _sprite.border;
            }
            else
            {
                outer = Vector4.zero;
                inner = Vector4.zero;
                padding = Vector4.zero;
                border = Vector4.zero;
            }

            Rect rect = GetPixelAdjustedRect();

            Vector4 adjustedBorders = GetAdjustedBorders(border / _pixelsPerUnitMultiplier, rect);
            padding = padding / _pixelsPerUnitMultiplier;

            s_VertScratch[0] = new Vector2(padding.x, padding.y);
            s_VertScratch[3] = new Vector2(rect.width - padding.z, rect.height - padding.w);

            s_VertScratch[1].x = adjustedBorders.x;
            s_VertScratch[1].y = adjustedBorders.y;

            s_VertScratch[2].x = rect.width - adjustedBorders.z;
            s_VertScratch[2].y = rect.height - adjustedBorders.w;

            for (int i = 0; i < 4; ++i)
            {
                s_VertScratch[i].x += rect.x;
                s_VertScratch[i].y += rect.y;
            }

            s_UVScratch[0] = new Vector2(outer.x, outer.y);
            s_UVScratch[1] = new Vector2(inner.x, inner.y);
            s_UVScratch[2] = new Vector2(inner.z, inner.w);
            s_UVScratch[3] = new Vector2(outer.z, outer.w);

            vh.Clear();

            for (int x = 0; x < 3; ++x)
            {
                int x2 = x + 1;

                for (int y = 0; y < 3; ++y)
                {
                    if (!_fillCenter && x == 1 && y == 1)
                        continue;

                    int y2 = y + 1;

                    if ((s_VertScratch[x2].x - s_VertScratch[x].x <= 0) || (s_VertScratch[y2].y - s_VertScratch[y].y <= 0))
                        continue;

                    Vector2 bottomLeft = new Vector2(s_VertScratch[x].x, s_VertScratch[y].y);
                    Vector2 topRight = new Vector2(s_VertScratch[x2].x, s_VertScratch[y2].y);

                    Vector2 normalizedBottomLeft = new Vector2(
                        (bottomLeft.x - rect.x) / rect.width,
                        (bottomLeft.y - rect.y) / rect.height
                    );
                    Vector2 normalizedTopLeft = new Vector2(
                        (bottomLeft.x - rect.x) / rect.width,
                        (topRight.y - rect.y) / rect.height
                    );
                    Vector2 normalizedTopRight = new Vector2(
                        (topRight.x - rect.x) / rect.width,
                        (topRight.y - rect.y) / rect.height
                    );
                    Vector2 normalizedBottomRight = new Vector2(
                        (topRight.x - rect.x) / rect.width,
                        (bottomLeft.y - rect.y) / rect.height
                    );

                    Color colorBL = _gradient.Evaluate(GetGradientTime(normalizedBottomLeft)) * color;
                    Color colorTL = _gradient.Evaluate(GetGradientTime(normalizedTopLeft)) * color;
                    Color colorTR = _gradient.Evaluate(GetGradientTime(normalizedTopRight)) * color;
                    Color colorBR = _gradient.Evaluate(GetGradientTime(normalizedBottomRight)) * color;

                    AddQuad(vh,
                        bottomLeft,
                        topRight,
                        colorBL, colorTL, colorTR, colorBR,
                        new Vector2(s_UVScratch[x].x, s_UVScratch[y].y),
                        new Vector2(s_UVScratch[x2].x, s_UVScratch[y2].y));
                }
            }
        }

        private Vector4 GetAdjustedBorders(Vector4 border, Rect rect)
        {
            Rect originalRect = rectTransform.rect;

            for (int axis = 0; axis <= 1; axis++)
            {
                float borderScaleRatio;

                if (originalRect.size[axis] != 0)
                {
                    borderScaleRatio = rect.size[axis] / originalRect.size[axis];
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }

                float combinedBorders = border[axis] + border[axis + 2];
                if (rect.size[axis] < combinedBorders && combinedBorders != 0)
                {
                    borderScaleRatio = rect.size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }
            return border;
        }

        private float GetGradientTime(Vector2 normalizedPos)
        {
            switch (_direction)
            {
                case GradientDirection.LeftToRight: return normalizedPos.x;
                case GradientDirection.RightToLeft: return 1 - normalizedPos.x;
                case GradientDirection.BottomToTop: return normalizedPos.y;
                case GradientDirection.TopToBottom: return 1 - normalizedPos.y;
                default: return normalizedPos.x;
            }
        }

        protected override Vector2 GetUVForNormalizedPosition(Vector2 position)
        {
            if (_sprite)
            {
                Vector4 outerUV = DataUtility.GetOuterUV(_sprite);
                return new Vector2(
                    Mathf.Lerp(outerUV.x, outerUV.z, position.x),
                    Mathf.Lerp(outerUV.y, outerUV.w, position.y)
                );
            }
            else
            {
                return position;
            }
        }

        private static void AddQuad(VertexHelper vertexHelper, Vector2 posMin, Vector2 posMax,
            Color colorBottomLeft, Color colorTopLeft, Color colorTopRight, Color colorBottomRight,
            Vector2 uvMin, Vector2 uvMax)
        {
            int startIndex = vertexHelper.currentVertCount;

            vertexHelper.AddVert(new Vector3(posMin.x, posMin.y, 0), colorBottomLeft, new Vector2(uvMin.x, uvMin.y));
            vertexHelper.AddVert(new Vector3(posMin.x, posMax.y, 0), colorTopLeft, new Vector2(uvMin.x, uvMax.y));
            vertexHelper.AddVert(new Vector3(posMax.x, posMax.y, 0), colorTopRight, new Vector2(uvMax.x, uvMax.y));
            vertexHelper.AddVert(new Vector3(posMax.x, posMin.y, 0), colorBottomRight, new Vector2(uvMax.x, uvMin.y));

            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }
    }
}
