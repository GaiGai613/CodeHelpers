using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace CodeHelpers.AI.BehaviorTrees.UIEditor
{
	[CustomEditor(typeof(BehaviorTreeBlueprintData))]
	public class TreeBlueprintCustomEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			if (!GUILayout.Button("Open Editor")) return;
			TreeGraphEditorWindow.Open((BehaviorTreeBlueprintData)target);
		}

		[OnOpenAsset]
		public static bool OpenEditor(int instanceId, int line)
		{
			var data = EditorUtility.InstanceIDToObject(instanceId) as BehaviorTreeBlueprintData;
			if (data == null) return false;

			TreeGraphEditorWindow.Open(data);
			return true;
		}
	}
}