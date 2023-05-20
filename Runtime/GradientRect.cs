using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gilzoide.GradientRect
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class GradientRect : MaskableGraphic
    {
        public enum GradientDirection
        {
            LeftToRight,
            RightToLeft,
            BottomToTop,
            TopToBottom,
        }

        [Header("Gradient")]
        [SerializeField] protected Gradient _gradient;
        [SerializeField] protected GradientDirection _direction;

        public Gradient Gradient
        {
            get => _gradient;
            set
            {
                if (_gradient == value)
                {
                    return;
                }
                _gradient = value;
                SetVerticesDirty();
            }
        }

        public GradientDirection Direction
        {
            get => _direction;
            set
            {
                if (_direction == value)
                {
                    return;
                }
                _direction = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            using (var enumerator = EnumerateGradient(_gradient).GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    return;
                }

                Rect rect = GetPixelAdjustedRect();
                Color tint = color;

                (float time1, Color color1) = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    (float time2, Color color2) = enumerator.Current;

                    Vector2 v1, v2, v3, v4;
                    Color c1, c2, c3, c4;
                    switch (_direction)
                    {
                        case GradientDirection.LeftToRight:
                            v1 = new Vector2(time1, 0);
                            v2 = new Vector2(time1, 1);
                            v3 = new Vector2(time2, 1);
                            v4 = new Vector2(time2, 0);
                            c1 = c2 = color1 * tint;
                            c3 = c4 = color2 * tint;
                            break;
                        
                        case GradientDirection.RightToLeft:
                            v1 = new Vector2(1 - time2, 0);
                            v2 = new Vector2(1 - time2, 1);
                            v3 = new Vector2(1 - time1, 1);
                            v4 = new Vector2(1 - time1, 0);
                            c1 = c2 = color2 * tint;
                            c3 = c4 = color1 * tint;
                            break;
                        
                        case GradientDirection.BottomToTop:
                            v1 = new Vector2(0, time1);
                            v2 = new Vector2(0, time2);
                            v3 = new Vector2(1, time2);
                            v4 = new Vector2(1, time1);
                            c1 = c4 = color1 * tint;
                            c2 = c3 = color2 * tint;
                            break;

                        case GradientDirection.TopToBottom:
                            v1 = new Vector2(0, 1 - time2);
                            v2 = new Vector2(0, 1 - time1);
                            v3 = new Vector2(1, 1 - time1);
                            v4 = new Vector2(1, 1 - time2);
                            c1 = c4 = color2 * tint;
                            c2 = c3 = color1 * tint;
                            break;
                            
                        default: throw new ArgumentOutOfRangeException(nameof(_direction));
                    }

                    int vertexCount = vh.currentVertCount;

                    vh.AddVert(Rect.NormalizedToPoint(rect, v1), c1, GetUVForNormalizedPosition(v1));
                    vh.AddVert(Rect.NormalizedToPoint(rect, v2), c2, GetUVForNormalizedPosition(v2));
                    vh.AddVert(Rect.NormalizedToPoint(rect, v3), c3, GetUVForNormalizedPosition(v3));
                    vh.AddVert(Rect.NormalizedToPoint(rect, v4), c4, GetUVForNormalizedPosition(v4));

                    vh.AddTriangle(vertexCount, vertexCount + 1, vertexCount + 2);
                    vh.AddTriangle(vertexCount + 2, vertexCount + 3, vertexCount);

                    (time1, color1) = (time2, color2);
                }
            }
        }

        protected virtual Vector2 GetUVForNormalizedPosition(Vector2 position)
        {
            return position;
        }

        public static IEnumerable<(float t, Color c)> EnumerateGradient(Gradient gradient)
        {
            if (gradient == null)
            {
                yield break;
            }

            float colorTime = 0;
            float alphaTime = 0;

            yield return (0, gradient.Evaluate(0));

            GradientColorKey[] colorKeys = gradient.colorKeys;
            GradientAlphaKey[] alphaKeys = gradient.alphaKeys;
            int colorIndex = colorKeys[0].time == 0 ? 1 : 0;
            int alphaIndex = alphaKeys[0].time == 0 ? 1 : 0;

            while (colorIndex < colorKeys.Length && alphaIndex < alphaKeys.Length)
            {
                colorTime = colorIndex < colorKeys.Length ? colorKeys[colorIndex].time : float.MaxValue;
                alphaTime = alphaIndex < alphaKeys.Length ? alphaKeys[alphaIndex].time : float.MaxValue;

                if (alphaTime == colorTime)
                {
                    alphaIndex++;
                    colorIndex++;
                    yield return (colorTime, gradient.Evaluate(colorTime));
                }
                else if (alphaTime < colorTime)
                {
                    alphaIndex++;
                    yield return (alphaTime, gradient.Evaluate(alphaTime));
                }
                else
                {
                    colorIndex++;
                    yield return (colorTime, gradient.Evaluate(colorTime));
                }
            }

            if (colorTime < 1 || alphaTime < 1)
            {
                yield return (1, gradient.Evaluate(1));
            }
        }
    }
}
