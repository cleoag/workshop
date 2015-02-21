using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

public class KinectToolbar : MonoBehaviour
{
    [MenuItem("Kinect/Create KinectMapper")]
    public static void MakeKinectMapper()
    {
        JointMapping.Create(Selection.activeGameObject);
    }
}

[CanEditMultipleObjects]
[CustomEditor(typeof(JointMapping))]
public class JointMapListEditor : Editor
{
    public SerializedObject jointMapperSO;

    public JointMapping jointMapList;

    public string[] jointTypeNames;

    public string[] boneNames;

    private int addSelectedTypeIndex;

    private int addSelectedBoneIndex;

    private List<int> deletedItems;
    private List<int> DeletedItemsList
    {
        get
        {
            if(this.deletedItems == null)
            {
                this.deletedItems = new List<int>(); ;
            }

            return this.deletedItems;
        }
    }

    public void OnEnable()
    {
        jointMapperSO = new SerializedObject(target);

        this.jointMapList = target as JointMapping;
        if (null != this.jointMapList)
        {
            this.jointTypeNames = this.jointMapList.JointTypeNames;
            if (null != this.jointMapList.BoneNames)
            {
                this.boneNames = this.jointMapList.BoneNames.ToArray();
            }
        }
    }

    public override void OnInspectorGUI()
    {
        // sync the serialized object with the component
        jointMapperSO.Update();

        if (null != this.jointMapList)
        {
            EditorGUI.BeginChangeCheck();
            var updatedModel = EditorGUILayout.ObjectField("Mapped Mesh:", this.jointMapList.Mesh, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
            if (EditorGUI.EndChangeCheck())
            {
                jointMapList.Mesh = updatedModel;
            }

            EditorGUILayout.BeginVertical();
            EditorGUI.indentLevel = 0;

            if (this.jointTypeNames != null && this.jointTypeNames != null && this.boneNames != null)
            {

                // toggle display of mappings
                foreach (var mapping in this.jointMapList.List)
                {
                    if (mapping.Bone != null)
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUI.BeginChangeCheck();
                        int boneIndex = Array.IndexOf<string>(this.boneNames, mapping.Bone.name);
                        int boneSelectedIndex = EditorGUILayout.Popup(mapping.Type.ToString(), boneIndex, this.boneNames);
                        if (EditorGUI.EndChangeCheck())
                        {
                            this.jointMapList.UpdateMapping(mapping, boneSelectedIndex);
                        }
                        if (GUILayout.Toggle(false, ""))
                        {
                            this.DeletedItemsList.Add(boneSelectedIndex);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }

                // mapping area
                EditorGUILayout.BeginHorizontal();
                this.addSelectedTypeIndex = EditorGUILayout.Popup(this.addSelectedTypeIndex, this.jointTypeNames);
                this.addSelectedBoneIndex = EditorGUILayout.Popup(this.addSelectedBoneIndex, this.boneNames);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Map", GUILayout.Width(50.0f)))
            {
                if(this.addSelectedTypeIndex != -1 || this.addSelectedBoneIndex != -1)
                {
                    jointMapList.AddMapping(jointTypeNames[this.addSelectedTypeIndex], boneNames[this.addSelectedBoneIndex]);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            //if (GUILayout.Button("Reset Transformations"))
            //{
            //    //jointMapListGO.Reset();
            //}
        }

        EditorGUILayout.Space();

        DrawDefaultInspector();

        if (GUI.changed)
        {
            foreach (var boneIndex in this.DeletedItemsList)
            {
                this.jointMapList.RemoveMapping(boneIndex);
            }
            this.DeletedItemsList.Clear();

            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(this.jointMapList);
        }

        // apply any changes to the serialized object
        jointMapperSO.ApplyModifiedProperties();
    }

//    public static string TextField(string label, string text)
//    {
//        var textDimensions = GUI.skin.label.CalcSize(new GUIContent(label));
//        EditorGUIUtility.labelWidth = textDimensions.x;
//        return EditorGUILayout.TextField(label, text);
//    }
}
