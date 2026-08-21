using System;
using MMDPlayerForVR.PmxPhysics;
using UnityEditor;
using UnityEngine;

public sealed class PMXPhysicsToUnityConverterWindow : EditorWindow
{
    private string _pmxFilePath = string.Empty;
    private Transform _armatureRoot;

    [MenuItem("Tools/MMD Player/PMX Physics Converter")]
    private static void Open()
    {
        GetWindow<PMXPhysicsToUnityConverterWindow>("PMX Physics");
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField("PMX Physics Converter", EditorStyles.boldLabel);
            _armatureRoot = (Transform)EditorGUILayout.ObjectField("Armature Root", _armatureRoot, typeof(Transform), true);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField("PMX File", _pmxFilePath);
                if (GUILayout.Button("Select", GUILayout.Width(72f)))
                {
                    string path = EditorUtility.OpenFilePanel("Select PMX file", string.Empty, "pmx");
                    if (!string.IsNullOrEmpty(path))
                    {
                        _pmxFilePath = path;
                    }
                }
            }

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_pmxFilePath) || _armatureRoot == null);
            if (GUILayout.Button("Apply PMX Physics"))
            {
                Apply();
            }
            EditorGUI.EndDisabledGroup();
        }
    }

    private void Apply()
    {
        try
        {
            PmxPhysicsData data = new PmxPhysicsReader().Read(_pmxFilePath);
            PmxPhysicsApplyResult result = new PmxPhysicsApplier().Apply(data, _armatureRoot);
            Debug.Log($"PMX physics applied. RigidBodies: {result.CreatedRigidBodyCount}/{result.RigidBodyCount}, Joints: {result.CreatedJointCount}/{result.JointCount}, SkippedBodies: {result.SkippedRigidBodyCount}, SkippedJoints: {result.SkippedJointCount}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"PMX physics conversion failed: {exception}");
        }
    }
}
