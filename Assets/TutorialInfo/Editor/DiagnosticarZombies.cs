#if UNITY_EDITOR
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Menú ZPark > Diagnosticar zombies
// Revisa cada prefab Zombie_<Tipo> y escribe en la Console un informe de su animación.
public static class DiagnosticarZombies
{
    static readonly string[] Tipos = { "Infectado", "Corredor", "Acechador" };

    [MenuItem("ZPark/Diagnosticar zombies")]
    static void Diagnosticar()
    {
        foreach (string tipo in Tipos) Revisar(tipo);
    }

    static void Revisar(string tipo)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("===== " + tipo + " =====");

        // Importador del FBX
        string fbx = "Assets/Art/Zombies/" + tipo + "/" + tipo + ".fbx";
        ModelImporter imp = AssetImporter.GetAtPath(fbx) as ModelImporter;
        if (imp == null) { Debug.LogWarning(sb + "No encontré " + fbx); return; }
        sb.AppendLine("Rig: " + imp.animationType + " | Avatar: " + imp.avatarSetup +
                      " | Import Animation: " + imp.importAnimation + " | Optimize GameObjects: " + imp.optimizeGameObjects);
        foreach (ModelImporterClipAnimation c in imp.clipAnimations)
            sb.AppendLine("  clip " + c.name + "  loop=" + c.loopTime + "  frames " + c.firstFrame + "-" + c.lastFrame);

        // Clips reales dentro del FBX y si sus curvas apuntan a huesos que existen
        GameObject modeloFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToArray();
        foreach (AnimationClip clip in clips)
        {
            EditorCurveBinding[] b = AnimationUtility.GetCurveBindings(clip);
            int ok = b.Count(x => x.path == "" || modeloFbx.transform.Find(x.path) != null);
            sb.AppendLine("  " + clip.name + ": " + clip.length.ToString("0.00") + " s, curvas " + b.Length +
                          ", con hueso encontrado " + ok + (b.Length > 0 && ok == 0 ? "  <-- NO COINCIDEN" : ""));
            if (b.Length > 0 && ok < b.Length)
            {
                EditorCurveBinding malo = b.First(x => x.path != "" && modeloFbx.transform.Find(x.path) == null);
                sb.AppendLine("     ejemplo de ruta que no existe: " + malo.path);
            }
        }

        // Prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie_" + tipo + ".prefab");
        if (prefab == null) { sb.AppendLine("Prefab: NO EXISTE"); Debug.LogWarning(sb.ToString()); return; }

        Animator[] anims = prefab.GetComponentsInChildren<Animator>(true);
        sb.AppendLine("Animators en el prefab: " + anims.Length);
        foreach (Animator a in anims)
        {
            AnimatorController ctrl = a.runtimeAnimatorController as AnimatorController;
            string estados = ctrl != null
                ? string.Join(", ", ctrl.layers[0].stateMachine.states.Select(s => s.state.name +
                    (s.state.motion == null ? "(SIN CLIP)" : "")))
                : "-";
            sb.AppendLine("  en '" + a.gameObject.name + "': activo=" + a.enabled +
                          " | controller=" + (ctrl != null ? ctrl.name : "NINGUNO") +
                          " | avatar=" + (a.avatar != null ? a.avatar.name + (a.avatar.isValid ? " (válido)" : " (INVÁLIDO)") : "NINGUNO") +
                          " | estados: " + estados);
        }

        SkinnedMeshRenderer[] smr = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        sb.AppendLine("SkinnedMeshRenderer: " + smr.Length + (smr.Length > 0 ? " | huesos: " + smr[0].bones.Length +
                      " | root: " + (smr[0].rootBone != null ? smr[0].rootBone.name : "NINGUNO") : ""));

        Debug.Log(sb.ToString());
    }
}
#endif
