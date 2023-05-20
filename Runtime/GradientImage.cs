using UnityEngine;

namespace Gilzoide.GradientRect
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class GradientImage : GradientRect
    {
        [Header("Texture")]
        [SerializeField] protected Sprite _sprite;

        /// <summary>Sprite used to draw Image.</summary>
        /// <remarks>For now, the only fill supported is simple mode, no slicing nor tiling.</remarks>
        public Sprite Sprite
        {
            get => _sprite;
            set
            {
                if (_sprite == value)
                {
                    return;
                }

                _sprite = value;
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        public override Texture mainTexture
        {
            get
            {
                if (_sprite == null)
                {
                    if (material != null && material.mainTexture != null)
                    {
                        return material.mainTexture;
                    }
                    return s_WhiteTexture;
                }

                return _sprite.texture;
            }
        }

        protected override Vector2 GetUVForNormalizedPosition(Vector2 position)
        {
            if (_sprite)
            {
                Vector4 outerUV = UnityEngine.Sprites.DataUtility.GetOuterUV(_sprite);
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
    }
}
