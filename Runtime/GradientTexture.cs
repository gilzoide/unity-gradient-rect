using UnityEngine;

namespace Gilzoide.GradientRect
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class GradientTexture : GradientRect
    {
        [Header("Texture")]
        [SerializeField] protected Texture _texture;
        [SerializeField] protected Rect _UVRect = new Rect(0, 0, 1, 1);

        /// <summary>Texture used to draw Image.</summary>
        public Texture Texture
        {
            get => _texture;
            set
            {
                if (_texture == value)
                {
                    return;
                }

                _texture = value;
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        /// <summary>UV rectangle used to sample <see cref="Texture"/>.</summary>
        public Rect UVRect
        {
            get => _UVRect;
            set
            {
                if (_UVRect == value)
                {
                    return;
                }

                _UVRect = value;
                SetVerticesDirty();
            }
        }

        public override Texture mainTexture
        {
            get
            {
                if (_texture == null)
                {
                    if (material != null && material.mainTexture != null)
                    {
                        return material.mainTexture;
                    }
                    return s_WhiteTexture;
                }

                return _texture;
            }
        }

        protected override Vector2 GetUVForNormalizedPosition(Vector2 position)
        {
            return Rect.NormalizedToPoint(_UVRect, position);
        }
    }
}
