using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[InitializeOnLoad]
public static class AnimatorManipulationSceneViewListener
{
    public static AnimatorManipulationWindow window = null;

    static Vector2 openWindowButtonSize = new Vector2(50, 500);
    static Vector2 replaceButtonSize = new Vector2(50, 50);

    static AnimatorManipulationSceneViewListener() 
    { 
        SceneView.duringSceneGui -= SceneViewGUIUpdate; 
        SceneView.duringSceneGui += SceneViewGUIUpdate; 
    }

    private static void SceneViewGUIUpdate(SceneView sceneview)
    {
        if (Selection.activeGameObject && Selection.activeGameObject.TryGetComponent(out Animator anim))
        {
            Rect sceneRect = SceneView.lastActiveSceneView.camera.pixelRect;

            if (!window)
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(new Vector2((sceneRect.width / 2) - (openWindowButtonSize.x / 2), 10), openWindowButtonSize));
                if (GUILayout.Button(EditorGUIUtility.IconContent("winbtn_win_restore_a@2x")))
                {
                    AnimatorManipulationWindow.Open();
                    window.SelectAnimator(anim);
                }
                GUILayout.EndArea();
                Handles.EndGUI();
            }
            else if (!window.IsAlreadySelected(anim))
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(new Vector2((sceneRect.width / 2) - (replaceButtonSize.x / 2), 10), replaceButtonSize));
                if (GUILayout.Button(EditorGUIUtility.IconContent("RotateTool@2x")))
                    window.SelectAnimator(anim);
                GUILayout.EndArea();
                Handles.EndGUI();
            }
        }
    }
}
