using UnityEditor;
using UnityEditor.UI;

namespace Gilzoide.GradientRect.Editor
{
    [CustomEditor(typeof(GradientRect))]
    public class GradientRectEditor : GraphicEditor
    {
        private SerializedProperty _gradient;
        private SerializedProperty _direction;

        protected override void OnEnable()
        {
            base.OnEnable();
            _gradient = serializedObject.FindProperty("_gradient");
            _direction = serializedObject.FindProperty("_direction");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AppearanceControlsGUI();
            DrawGradientPropertys();
            RaycastControlsGUI();
            MaskableControlsGUI();

            serializedObject.ApplyModifiedProperties();
        }


        protected virtual void DrawGradientPropertys()
        {
            EditorGUILayout.PropertyField(_gradient);
            EditorGUILayout.PropertyField(_direction);
        }
    }
}