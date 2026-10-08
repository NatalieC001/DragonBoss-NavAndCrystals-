using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BossNavigator))]
public class BossNavigatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var nav = (BossNavigator)target;

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Navigator Actions", EditorStyles.boldLabel);

        //if (GUILayout.Button("Freestyle (Area)"))
        //    nav.Freestyle();

        if (GUILayout.Button("Freestyle To Player"))
            nav.FreestyleToPlayer();

        if (GUILayout.Button("Move To Nearest Observation Spline"))
            nav.MoveToNearestObservation();

        if (GUILayout.Button("Move To Nearest Escape Spline"))
            nav.MoveToNearestEscape();

        if (GUILayout.Button("Hold Position"))
            nav.HoldPosition();

        if (GUILayout.Button("Defend Crystal"))
            nav.DefendCrystalCommand();

        if (Application.isPlaying) Repaint();
    }
}