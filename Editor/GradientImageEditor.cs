using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.UI;

namespace Gilzoide.GradientRect.Editor
{
    [CustomEditor(typeof(GradientImage))]
    public class GradientImageEditor : GradientRectEditor
    {
        private SerializedProperty _sprite;
        private SerializedProperty _type;
        private SerializedProperty _fillCenter;
        private SerializedProperty _pixelsPerUnitMultiplier;

        private AnimBool m_ShowSlicedOrTiled;
        private AnimBool m_ShowSliced;
        private AnimBool m_ShowTiled;
        private AnimBool m_ShowFilled;

        private AnimBool m_ShowType;

        protected override void OnEnable()
        {
            base.OnEnable();

            _sprite = serializedObject.FindProperty("_sprite");
            _type = serializedObject.FindProperty("_type");
            _fillCenter = serializedObject.FindProperty("_fillCenter");
            _pixelsPerUnitMultiplier = serializedObject.FindProperty("_pixelsPerUnitMultiplier");

            Image.Type enumValueIndex = (Image.Type)_type.enumValueIndex;

            m_ShowSlicedOrTiled = new AnimBool(!_type.hasMultipleDifferentValues && enumValueIndex == Image.Type.Sliced);
            m_ShowSlicedOrTiled.valueChanged.AddListener(Repaint);

            m_ShowSliced = new AnimBool(!_type.hasMultipleDifferentValues && enumValueIndex == Image.Type.Sliced);
            m_ShowSliced.valueChanged.AddListener(Repaint);

            m_ShowTiled = new AnimBool(!_type.hasMultipleDifferentValues && enumValueIndex == Image.Type.Tiled);
            m_ShowTiled.valueChanged.AddListener(Repaint);

            m_ShowFilled = new AnimBool(!_type.hasMultipleDifferentValues && enumValueIndex == Image.Type.Filled);
            m_ShowFilled.valueChanged.AddListener(Repaint);

            m_ShowType = new AnimBool(_sprite.objectReferenceValue != null);
            m_ShowType.valueChanged.AddListener(Repaint);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_ShowType.valueChanged.RemoveListener(Repaint);
            m_ShowSlicedOrTiled.valueChanged.RemoveListener(Repaint);
            m_ShowSliced.valueChanged.RemoveListener(Repaint);

            m_ShowTiled.valueChanged.RemoveListener(Repaint);
            m_ShowFilled.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SpriteGUI();

            AppearanceControlsGUI();
            DrawGradientPropertys();
            RaycastControlsGUI();
            MaskableControlsGUI();

            m_ShowType.target = _sprite.objectReferenceValue != null;
            if (EditorGUILayout.BeginFadeGroup(m_ShowType.faded))
            {
                TypeGUI();
            }
            EditorGUILayout.EndFadeGroup();

            serializedObject.ApplyModifiedProperties();
        }

        protected void SpriteGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_sprite);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Sprite sprite = _sprite.objectReferenceValue as Sprite;
            if (sprite)
            {
                Image.Type enumValueIndex = (Image.Type)_type.enumValueIndex;
                if (sprite.border.SqrMagnitude() > 0f)
                {
                    _type.enumValueIndex = 1;
                }
                else if (enumValueIndex == Image.Type.Sliced)
                {
                    _type.enumValueIndex = 0;
                }
            }
        }

        protected void TypeGUI()
        {
            EditorGUILayout.PropertyField(_type, EditorGUIUtility.TrTextContent("Image Type"));
            EditorGUI.indentLevel++;
            Image.Type enumValueIndex = (Image.Type)_type.enumValueIndex;
            bool flag = !_type.hasMultipleDifferentValues && (enumValueIndex == Image.Type.Sliced || enumValueIndex == Image.Type.Tiled);
            if (flag && targets.Length > 1)
            {
                flag = targets.Select((Object obj) => obj as Image).All((Image img) => img.hasBorder);
            }

            m_ShowSlicedOrTiled.target = flag;
            m_ShowSliced.target = flag && !_type.hasMultipleDifferentValues && enumValueIndex == Image.Type.Sliced;
            m_ShowTiled.target = flag && !_type.hasMultipleDifferentValues && enumValueIndex == Image.Type.Tiled;
            m_ShowFilled.target = !_type.hasMultipleDifferentValues && enumValueIndex == Image.Type.Filled;

            GradientImage image = target as GradientImage;

            if (EditorGUILayout.BeginFadeGroup(m_ShowSliced.faded))
            //if (EditorGUILayout.BeginFadeGroup(m_ShowSlicedOrTiled.faded))
            {
                if (image.HasBorder)
                {
                    EditorGUILayout.PropertyField(_fillCenter);
                }

                EditorGUILayout.PropertyField(_pixelsPerUnitMultiplier);

                _pixelsPerUnitMultiplier.floatValue = Mathf.Max(0.01f, _pixelsPerUnitMultiplier.floatValue);
            }
            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(m_ShowSliced.faded) && image.Sprite != null && !image.HasBorder)
            {
                EditorGUILayout.HelpBox("This Image doesn't have a border.", MessageType.Warning);
            }
            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(m_ShowTiled.faded) && image.Sprite != null && !image.HasBorder && ((image.Sprite.texture != null && image.Sprite.texture.wrapMode != TextureWrapMode.Repeat) || image.Sprite.packed))
            {
                EditorGUILayout.HelpBox("This type unsoported to paint image.", MessageType.Error);
                EditorGUILayout.HelpBox("It looks like you want to tile a sprite with no border. It would be more efficient to modify the Sprite properties, clear the Packing tag and set the Wrap mode to Repeat.", MessageType.Warning);
            }
            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(m_ShowFilled.faded))
            {
                // TODO: code for editor for filled type image
                EditorGUILayout.HelpBox("This type unsoported to paint image.", MessageType.Error);
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUI.indentLevel--;
        }
    }
}