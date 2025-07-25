using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AnimationPathRebinder
{
    [MenuItem("Tools/Rebind Animation Paths")]
    public static void RebindAnimationPaths()
    {
        var clip = Selection.activeObject as AnimationClip;
        if (clip == null)
        {
            Debug.LogError("Please select an AnimationClip");
            return;
        }

        var bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (var binding in bindings)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

            var newBinding = binding;
            // 改路徑：例如從 "" 改到 "SubObject"
            if (binding.path == "")
                newBinding.path = "SubObject";

            AnimationUtility.SetEditorCurve(clip, binding, null); // 先移除原本
            AnimationUtility.SetEditorCurve(clip, newBinding, curve); // 再加上新的
        }

        Debug.Log("Rebinding complete.");
    }
}