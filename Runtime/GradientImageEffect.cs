using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gilzoide.GradientRect
{
    [AddComponentMenu("UI/Effects/Gradient Image Effect")]
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class GradientImageEffect : BaseMeshEffect
    {
        // VertexHelper rejects meshes with 65,000 or more vertices.
        private const int VertexLimit = 65000;

        [SerializeField] private Gradient _gradient = new Gradient();
        [SerializeField, Tooltip("Degrees counterclockwise from left to right.")]
        private float _angle;

        private readonly List<float> _stops = new List<float>();
        private readonly List<GradientColorKey> _colorSamples = new List<GradientColorKey>();
        private readonly List<UIVertex> _source = new List<UIVertex>();
        private readonly List<UIVertex> _vertices = new List<UIVertex>();
        private readonly List<int> _indices = new List<int>();
        private readonly List<ProjectedVertex> _triangle = new List<ProjectedVertex>(4);
        private readonly List<ProjectedVertex> _remainder = new List<ProjectedVertex>(4);
        private readonly List<ProjectedVertex> _band = new List<ProjectedVertex>(5);

        public Gradient Gradient
        {
            get => _gradient;
            set
            {
                _gradient = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        /// <summary>Gradient angle in degrees counterclockwise from left to right.</summary>
        public float Angle
        {
            get => _angle;
            set
            {
                if (_angle.Equals(value))
                    return;

                _angle = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || _gradient == null || !(graphic is Image image) || mesh.currentIndexCount == 0 ||
                float.IsNaN(_angle) || float.IsInfinity(_angle))
                return;

            float radians = (_angle % 360f) * Mathf.Deg2Rad;
            var projection = new Projection(GetGradientRect(image, mesh),
                new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)));
            bool fixedColor = _gradient.mode == GradientMode.Fixed;
            CollectStops();

            // If the whole mesh lies in one band, keep its original indexed topology.
            var vertex = new UIVertex();
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                float time = projection.GetTime(vertex.position);
                min = Mathf.Min(min, time);
                max = Mathf.Max(max, time);
            }

            GetBandBounds(FindBand(min), out float lower, out float upper);
            if (max <= upper)
            {
                Color? bandColor = fixedColor ? GetFixedBandColor(lower, upper) : (Color?)null;
                TintOriginalMesh(mesh, projection, bandColor);
                return;
            }

            _vertices.Clear();
            _indices.Clear();
            if (mesh.currentIndexCount < VertexLimit && TrySubdivide(mesh, projection, fixedColor))
            {
                mesh.Clear();
                mesh.AddUIVertexStream(_vertices, _indices);
            }
            else
            {
                // Keep dense tiled images renderable when splitting would exceed the limit.
                TintOriginalMesh(mesh, projection, null);
            }
        }

        private static Rect GetGradientRect(Image image, VertexHelper mesh)
        {
            Rect rect = image.GetPixelAdjustedRect();
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

        private bool TrySubdivide(VertexHelper mesh, Projection projection, bool fixedColor)
        {
            _source.Clear();
            mesh.GetUIVertexStream(_source);

            for (int i = 0; i < _source.Count; i += 3)
            {
                _triangle.Clear();
                for (int j = 0; j < 3; j++)
                {
                    UIVertex vertex = _source[i + j];
                    _triangle.Add(new ProjectedVertex(vertex, projection.GetTime(vertex.position)));
                }

                if (!AppendTriangleBands(fixedColor))
                    return false;
            }

            return true;
        }

        private void CollectStops()
        {
            _stops.Clear();
            _colorSamples.Clear();
            _stops.Add(0f);
            _stops.Add(1f);

            GradientColorKey[] colorKeys = _gradient.colorKeys;
            bool sampleLinearColors = QualitySettings.activeColorSpace == ColorSpace.Linear &&
                _gradient.mode != GradientMode.Fixed;
            for (int i = 0; i < colorKeys.Length; i++)
            {
#if UNITY_2022_2_OR_NEWER
                // Approximate perceptual interpolation with more samples between color keys.
                if (i > 0 && _gradient.mode == GradientMode.PerceptualBlend)
                {
                    const int samplesPerInterval = 8;
                    for (int j = 1; j < samplesPerInterval; j++)
                        AddColorSample(Mathf.Lerp(colorKeys[i - 1].time, colorKeys[i].time,
                            (float)j / samplesPerInterval), sampleLinearColors);
                }
#endif
                AddColorSample(colorKeys[i].time, sampleLinearColors);
            }
            foreach (GradientAlphaKey key in _gradient.alphaKeys)
                _stops.Add(key.time);

            _stops.Sort();
            for (int i = _stops.Count - 1; i > 0; i--)
            {
                if (_stops[i] == _stops[i - 1])
                    _stops.RemoveAt(i);
            }
        }

        private void AddColorSample(float time, bool sampleLinearColors)
        {
            _stops.Add(time);
            if (sampleLinearColors && (_colorSamples.Count == 0 || _colorSamples[_colorSamples.Count - 1].time < time))
                _colorSamples.Add(new GradientColorKey(_gradient.Evaluate(time), time));
        }

        private Color32 TintVertex(Color32 vertexColor, float time, Color? bandColor)
        {
            Color tint = vertexColor;
            Color sampled = tint * (bandColor ?? _gradient.Evaluate(time));
            if (_colorSamples.Count < 2 || time <= _colorSamples[0].time ||
                time >= _colorSamples[_colorSamples.Count - 1].time)
                return sampled;

            int low = 0;
            int high = _colorSamples.Count - 1;
            while (high - low > 1)
            {
                int middle = (low + high) / 2;
                if (time < _colorSamples[middle].time)
                    high = middle;
                else
                    low = middle;
            }

            // Unity interpolates UI colors in linear space, but expects gamma-encoded vertex colors.
            // Sample in the same space so extra sprite vertices keep the original color ramp.
            GradientColorKey left = _colorSamples[low];
            GradientColorKey right = _colorSamples[high];
            float blend = (time - left.time) / (right.time - left.time);
            Color color = Color.LerpUnclamped((tint * left.color).linear, (tint * right.color).linear, blend).gamma;
            color.a = sampled.a;
            return color;
        }

        private int FindBand(float time)
        {
            int index = _stops.BinarySearch(time);
            return index >= 0 ? index + 1 : ~index;
        }

        private void GetBandBounds(int index, out float lower, out float upper)
        {
            lower = index == 0 ? float.NegativeInfinity : _stops[index - 1];
            upper = index == _stops.Count ? float.PositiveInfinity : _stops[index];
        }

        private Color GetFixedBandColor(float lower, float upper)
        {
            // Sample inside the band so both sides of a step retain their own color.
            return _gradient.Evaluate((Mathf.Clamp01(lower) + Mathf.Clamp01(upper)) * 0.5f);
        }

        private void TintOriginalMesh(VertexHelper mesh, Projection projection, Color? bandColor)
        {
            var vertex = new UIVertex();
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                vertex.color = TintVertex(vertex.color, projection.GetTime(vertex.position), bandColor);
                mesh.SetUIVertex(vertex, i);
            }
        }

        private bool AppendTriangleBands(bool fixedColor)
        {
            float min = Mathf.Min(_triangle[0].Time, Mathf.Min(_triangle[1].Time, _triangle[2].Time));
            float max = Mathf.Max(_triangle[0].Time, Mathf.Max(_triangle[1].Time, _triangle[2].Time));
            int bandIndex = FindBand(min);
            List<ProjectedVertex> polygon = _triangle;
            List<ProjectedVertex> remainder = _remainder;

            while (true)
            {
                GetBandBounds(bandIndex, out float lower, out float upper);
                bool lastBand = max <= upper;
                if (!lastBand)
                    Split(polygon, _band, remainder, upper);

                List<ProjectedVertex> points = lastBand ? polygon : _band;
                if (_vertices.Count + points.Count >= VertexLimit)
                    return false;

                int start = _vertices.Count;
                Color? bandColor = fixedColor ? GetFixedBandColor(lower, upper) : (Color?)null;
                foreach (ProjectedVertex point in points)
                {
                    UIVertex vertex = point.Vertex;
                    vertex.color = TintVertex(vertex.color, point.Time, bandColor);
                    _vertices.Add(vertex);
                }

                for (int i = 1; i + 1 < points.Count; i++)
                {
                    _indices.Add(start);
                    _indices.Add(start + i);
                    _indices.Add(start + i + 1);
                }

                if (lastBand)
                    return true;

                List<ProjectedVertex> previousPolygon = polygon;
                polygon = remainder;
                remainder = previousPolygon;
                bandIndex++;
            }
        }

        private static void Split(List<ProjectedVertex> input, List<ProjectedVertex> below,
            List<ProjectedVertex> above, float boundary)
        {
            below.Clear();
            above.Clear();
            ProjectedVertex previous = input[input.Count - 1];
            foreach (ProjectedVertex current in input)
            {
                // A vertex on the boundary already serves as the intersection.
                if ((previous.Time < boundary && current.Time > boundary) ||
                    (previous.Time > boundary && current.Time < boundary))
                {
                    ProjectedVertex intersection = ProjectedVertex.Intersect(previous, current, boundary);
                    below.Add(intersection);
                    above.Add(intersection);
                }

                if (current.Time <= boundary)
                    below.Add(current);
                if (current.Time >= boundary)
                    above.Add(current);

                previous = current;
            }
        }

        private readonly struct Projection
        {
            private readonly Vector2 _center;
            private readonly Vector2 _direction;
            private readonly float _length;

            public Projection(Rect rect, Vector2 direction)
            {
                _center = rect.center;
                _direction = direction;
                _length = rect.width * Mathf.Abs(direction.x) + rect.height * Mathf.Abs(direction.y);
            }

            public float GetTime(Vector3 position)
            {
                if (_length <= 0f)
                    return 0f;

                float time = Vector2.Dot((Vector2)position - _center, _direction) / _length + 0.5f;
                // Avoid tiny extra polygons when a corner rounds past the rect's endpoint.
                const float endpointTolerance = 0.000001f;
                if (Mathf.Abs(time) < endpointTolerance)
                    return 0f;
                if (Mathf.Abs(1f - time) < endpointTolerance)
                    return 1f;
                return time;
            }
        }

        private readonly struct ProjectedVertex
        {
            public readonly UIVertex Vertex;
            public readonly float Time;

            public ProjectedVertex(UIVertex vertex, float time)
            {
                Vertex = vertex;
                Time = time;
            }

            public static ProjectedVertex Intersect(ProjectedVertex a, ProjectedVertex b, float time)
            {
                // Use the same interpolation order on shared edges in either winding.
                if (a.Time > b.Time)
                    return Intersect(b, a, time);

                float t = (time - a.Time) / (b.Time - a.Time);
                return new ProjectedVertex(new UIVertex
                {
                    position = Vector3.LerpUnclamped(a.Vertex.position, b.Vertex.position, t),
                    color = Color.LerpUnclamped(a.Vertex.color, b.Vertex.color, t),
                    uv0 = Vector4.LerpUnclamped(a.Vertex.uv0, b.Vertex.uv0, t),
                    uv1 = Vector4.LerpUnclamped(a.Vertex.uv1, b.Vertex.uv1, t),
                    uv2 = Vector4.LerpUnclamped(a.Vertex.uv2, b.Vertex.uv2, t),
                    uv3 = Vector4.LerpUnclamped(a.Vertex.uv3, b.Vertex.uv3, t),
                    normal = Vector3.LerpUnclamped(a.Vertex.normal, b.Vertex.normal, t),
                    tangent = Vector4.LerpUnclamped(a.Vertex.tangent, b.Vertex.tangent, t),
                }, time);
            }
        }
    }
}
