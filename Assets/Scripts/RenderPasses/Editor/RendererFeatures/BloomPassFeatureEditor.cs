using System.Linq;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering.LWRP
{
    [CustomPropertyDrawer(typeof(BloomRenderTexture.BloomSettings), true)]
    internal class BloomPassFeatureEditor : PropertyDrawer
    {
        internal class Styles
        {
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static GUIContent callback = new GUIContent("Event", "Chose the Callback position for this render pass object.");

            //Headers
            public static GUIContent targetHeader = new GUIContent("Target", "Render source and target.");
            public static GUIContent bloomHeader = new GUIContent("Bloom", "Bloom settings.");

            //Target Settings
            public static GUIContent sourceState = new GUIContent("Source", "Blit source.");
            public static GUIContent sourceTextureId = new GUIContent("SrcTextureId", "Texture Idenitifier of the source render texture.");
            public static GUIContent destinationState = new GUIContent("Destination", "Blit destination.");
            public static GUIContent destinationTextureId = new GUIContent("DstTextureId", "Texture Idenitifier of the destination render texture.");

            //Bloom Settings
            public static GUIContent bloomIntensity = new GUIContent("Intensity", "Strength of the bloom filter. Values higher than 1 will make bloom contribute more energy to the final render.");
            public static GUIContent bloomThreshold = new GUIContent("Threshold", "Filters out pixels under this level of brightness. Value is in gamma-space.");
            public static GUIContent bloomSoftKnee = new GUIContent("SoftKnee", "Makes transitions between under/over-threshold gradual. 0 for a hard threshold, 1 for a soft threshold).");
            public static GUIContent bloomClamp = new GUIContent("Clamp", "Clamps pixels to control the bloom amount. Value is in gamma-space.");
            public static GUIContent bloomDiffusion = new GUIContent("Diffusion", "Changes the extent of veiling effects. For maximum quality, use integer values. Because this value changes the internal iteration count, You should not animating it as it may introduce issues with the perceived radius.");
            public static GUIContent bloomAnamorphicRatio = new GUIContent("AnamorphicRatio", "Distorts the bloom to give an anamorphic look. Negative values distort vertically, positive values distort horizontally.");
            public static GUIContent bloomColor = new GUIContent("Color", "Global tint of the bloom filter.");
            public static GUIContent bloomFastMode = new GUIContent("Fast Mode", "Boost performance by lowering the effect quality. This settings is meant to be used on mobile and other low-end platforms but can also provide a nice performance boost on desktops and consoles.");
            public static GUIContent bloomDirtIntensity = new GUIContent("Dirt Intensity", "The intensity of the lens dirtiness.");
        }

        //Headers and layout
        private HeaderBool m_TargetFoldout;
        private int m_TargetLines = 5;
        private HeaderBool m_BloomFoldout;
        private int m_BloomLines = 9;

        private bool firstTime = true;

        // Serialized Properties
        private SerializedProperty m_Callback;
        private SerializedProperty m_PassTag;
        //Blit props
        private SerializedProperty m_SourceState;
        private SerializedProperty m_SourceTextureId;
        private SerializedProperty m_DestinationState;
        private SerializedProperty m_DestinationTextureId;
        //Bloom props
        private SerializedProperty m_BloomSettings;
        private SerializedProperty m_Intensity;
        private SerializedProperty m_Threshold;
        private SerializedProperty m_SoftKnee;
        private SerializedProperty m_Clamp;
        private SerializedProperty m_Diffusion;
        private SerializedProperty m_AnamorphicRatio;
        private SerializedProperty m_Color;
        private SerializedProperty m_FastMode;
        private SerializedProperty m_DirtIntensity;

        private void Init(SerializedProperty property)
        {
            //Header bools
            var key = $"{this.ToString().Split('.').Last()}.{property.serializedObject.targetObject.name}";
            m_TargetFoldout = new HeaderBool($"{key}.TargetFoldout", true);
            m_BloomFoldout = new HeaderBool($"{key}.BloomFoldout", true);

            m_Callback = property.FindPropertyRelative("Event");
            m_PassTag = property.FindPropertyRelative("passTag");
            //Blit props
            m_SourceState = property.FindPropertyRelative("source");
            m_SourceTextureId = property.FindPropertyRelative("srcTextureId");
            m_DestinationState = property.FindPropertyRelative("destination");
            m_DestinationTextureId = property.FindPropertyRelative("dstTextureId");

            m_BloomSettings = property.FindPropertyRelative("customBloomSettings");
            m_Intensity = m_BloomSettings.FindPropertyRelative("intensity");
            m_Threshold = m_BloomSettings.FindPropertyRelative("threshold");
            m_SoftKnee = m_BloomSettings.FindPropertyRelative("softKnee");
            m_Clamp = m_BloomSettings.FindPropertyRelative("clamp");
            m_Diffusion = m_BloomSettings.FindPropertyRelative("diffusion");
            m_AnamorphicRatio = m_BloomSettings.FindPropertyRelative("anamorphicRatio");
            m_Color = m_BloomSettings.FindPropertyRelative("color");
            m_FastMode = m_BloomSettings.FindPropertyRelative("fastMode");
            m_DirtIntensity = m_BloomSettings.FindPropertyRelative("dirtIntensity");

            firstTime = false;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginProperty(rect, label, property);
            if (firstTime)
                Init(property);

            var passName = property.serializedObject.FindProperty("m_Name").stringValue;
            if (passName != m_PassTag.stringValue)
            {
                m_PassTag.stringValue = passName;
                property.serializedObject.ApplyModifiedProperties();
            }

            //Forward Callbacks
            EditorGUI.PropertyField(rect, m_Callback, Styles.callback);
            rect.y += Styles.defaultLineSpace;

            DoTarget(ref rect);

            DoBloom(ref rect);

            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();
        }

        void DoTarget(ref Rect rect)
        {
            m_TargetFoldout.value = EditorGUI.Foldout(rect, m_TargetFoldout.value, Styles.targetHeader, true);
            SaveHeaderBool(m_TargetFoldout);
            rect.y += Styles.defaultLineSpace;
            if (m_TargetFoldout.value)
            {
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(rect, m_SourceState, Styles.sourceState);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.BeginDisabledGroup(m_SourceState.intValue == 0);
                EditorGUI.PropertyField(rect, m_SourceTextureId, Styles.sourceTextureId);
                rect.y += Styles.defaultLineSpace;
                EditorGUI.EndDisabledGroup();

                EditorGUI.PropertyField(rect, m_DestinationState, Styles.destinationState);
                rect.y += Styles.defaultLineSpace;

                //EditorGUI.BeginDisabledGroup(m_DestinationState.intValue == 0);
                EditorGUI.PropertyField(rect, m_DestinationTextureId, Styles.destinationTextureId);
                rect.y += Styles.defaultLineSpace;
                //EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }
        }

        void DoBloom(ref Rect rect)
        {
            m_BloomFoldout.value = EditorGUI.Foldout(rect, m_BloomFoldout.value, Styles.bloomHeader, true);
            SaveHeaderBool(m_BloomFoldout);
            rect.y += Styles.defaultLineSpace;
            if (m_BloomFoldout.value)
            {
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(rect, m_Intensity, Styles.bloomIntensity);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.PropertyField(rect, m_Threshold, Styles.bloomThreshold);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.PropertyField(rect, m_SoftKnee, Styles.bloomSoftKnee);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.PropertyField(rect, m_Clamp, Styles.bloomClamp);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.PropertyField(rect, m_Diffusion, Styles.bloomDiffusion);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.PropertyField(rect, m_AnamorphicRatio, Styles.bloomAnamorphicRatio);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.PropertyField(rect, m_Color, Styles.bloomColor);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.PropertyField(rect, m_FastMode, Styles.bloomFastMode);
                rect.y += Styles.defaultLineSpace;
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = Styles.defaultLineSpace;

            if (!firstTime)
            {
                height += Styles.defaultLineSpace * (m_TargetFoldout.value ? m_TargetLines : 1);
                height += Styles.defaultLineSpace * (m_BloomFoldout.value ? m_BloomLines : 1);
            }

            return height;
        }

        private void SaveHeaderBool(HeaderBool boolObj)
        {
            EditorPrefs.SetBool(boolObj.key, boolObj.value);
        }

        class HeaderBool
        {
            public string key;
            public bool value;

            public HeaderBool(string _key, bool _default = false)
            {
                key = _key;
                if (EditorPrefs.HasKey(key))
                    value = EditorPrefs.GetBool(key);
                else
                    value = _default;
                EditorPrefs.SetBool(key, value);
            }
        }
    }
}
