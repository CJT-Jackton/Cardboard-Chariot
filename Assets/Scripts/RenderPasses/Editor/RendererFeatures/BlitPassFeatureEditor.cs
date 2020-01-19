using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering.LWRP
{
    [CustomPropertyDrawer(typeof(Blit.BlitSettings), true)]
    internal class BlitPassFeatureEditor : PropertyDrawer
    {
        internal class Styles
        {
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static GUIContent callback = new GUIContent("Event", "Chose the Callback position for this render pass object.");

            //Headers
            public static GUIContent overrideHeader = new GUIContent("Overrides", "Different parts fo the rendering that you can choose to override.");
            public static GUIContent targetHeader = new GUIContent("Target", "Render source and target.");

            //Render Options
            public static GUIContent overrideMaterial = new GUIContent("Blit Material", "Chose an blit material.");
            public static GUIContent overrideMaterialPass = new GUIContent("Pass Index", "The pass index for the blit material to use.");

            //Blit Settings
            public static GUIContent sourceState = new GUIContent("Source", "Blit source.");
            public static GUIContent sourceTextureId = new GUIContent("SrcTextureId", "Texture Idenitifier of the source render texture.");
            public static GUIContent destinationState = new GUIContent("Destination", "Blit destination.");
            public static GUIContent destinationTextureId = new GUIContent("DstTextureId", "Texture Idenitifier of the destination render texture.");
        }

        //Headers and layout
        private HeaderBool m_OverrideFoldout;
        private int m_MaterialLines = 2;
        private HeaderBool m_TargetFoldout;
        private int m_TargetLines = 5;

        private bool firstTime = true;

        // Serialized Properties
        private SerializedProperty m_Callback;
        private SerializedProperty m_PassTag;
        //Render props
        private SerializedProperty m_BlitMaterial;
        private SerializedProperty m_BlitMaterialPass;
        //Blit props
        private SerializedProperty m_SourceState;
        private SerializedProperty m_SourceTextureId;
        private SerializedProperty m_DestinationState;
        private SerializedProperty m_DestinationTextureId;

        private void Init(SerializedProperty property)
        {
            //Header bools
            var key = $"{this.ToString().Split('.').Last()}.{property.serializedObject.targetObject.name}";
            m_OverrideFoldout = new HeaderBool($"{key}.OverrideFoldout", true);
            m_TargetFoldout = new HeaderBool($"{key}.TargetFoldout", true);

            m_Callback = property.FindPropertyRelative("Event");
            m_PassTag = property.FindPropertyRelative("passTag");
            //Render options
            m_BlitMaterial = property.FindPropertyRelative("blitMaterial");
            m_BlitMaterialPass = property.FindPropertyRelative("blitMaterialPassIndex");
            //Blit props
            m_SourceState = property.FindPropertyRelative("source");
            m_SourceTextureId = property.FindPropertyRelative("srcTextureId");
            m_DestinationState = property.FindPropertyRelative("destination");
            m_DestinationTextureId = property.FindPropertyRelative("dstTextureId");

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

            DoOverride(ref rect);

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

                EditorGUI.BeginDisabledGroup(m_DestinationState.intValue == 0);
                EditorGUI.PropertyField(rect, m_DestinationTextureId, Styles.destinationTextureId);
                rect.y += Styles.defaultLineSpace;
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }
        }

        void DoOverride(ref Rect rect)
        {
            m_OverrideFoldout.value = EditorGUI.Foldout(rect, m_OverrideFoldout.value, Styles.overrideHeader, true);
            SaveHeaderBool(m_OverrideFoldout);
            rect.y += Styles.defaultLineSpace;
            if (m_OverrideFoldout.value)
            {
                EditorGUI.indentLevel++;
                DoMaterialOverride(ref rect);
                EditorGUI.indentLevel--;
            }
        }

        void DoMaterialOverride(ref Rect rect)
        {
            //Override material
            EditorGUI.PropertyField(rect, m_BlitMaterial, Styles.overrideMaterial);
            if (m_BlitMaterial.objectReferenceValue)
            {
                rect.y += Styles.defaultLineSpace;
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, m_BlitMaterialPass, Styles.overrideMaterialPass);
                if (EditorGUI.EndChangeCheck())
                    m_BlitMaterialPass.intValue = Mathf.Max(0, m_BlitMaterialPass.intValue);
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = Styles.defaultLineSpace;

            if (!firstTime)
            {
                height += Styles.defaultLineSpace * (m_TargetFoldout.value ? m_TargetLines : 1);

                height += Styles.defaultLineSpace; // add line for overrides dropdown
                if (m_OverrideFoldout.value)
                {
                    height += Styles.defaultLineSpace * (m_BlitMaterial.objectReferenceValue != null ? m_MaterialLines : 1);
                }
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
