#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Menú ZPark > Configurar zombies 3D
// Toma los modelos de Assets/Art/Zombies/<Tipo>/<Tipo>.fbx y por cada uno:
//  1. Ajusta el importador (escala 1, sin cámaras/luces, rig Generic, bucle en Caminar/Correr/Quieto).
//  2. Crea un material URP/Lit con <Tipo>_Color.png (así no se ve magenta).
//  3. Crea un Animator Controller con todas sus animaciones.
//  4. Crea el prefab Assets/Prefabs/Zombie_<Tipo>.prefab (collider + script Zombie + modelo animado).
//  5. Lo asigna en Assets/Data/<Tipo>.asset (campo Modelo).
public static class ConfigurarZombies3D
{
    const string Carpeta = "Assets/Art/Zombies";
    static readonly string[] Tipos = { "Infectado", "Corredor", "Acechador" };
    static readonly string[] AnimsEnBucle = { "Caminar", "Correr", "Quieto" };

    // Giro del modelo dentro del prefab para que mire hacia adelante (hacia el jugador).
    // Si los zombies caminan de espaldas, cambiar entre 180 y 0 y volver a correr el menú.
    const float GiroModelo = 0f;

    [MenuItem("ZPark/Configurar zombies 3D")]
    static void Configurar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");

        List<string> listos = new List<string>();
        List<string> faltan = new List<string>();

        foreach (string tipo in Tipos)
        {
            string fbx = Carpeta + "/" + tipo + "/" + tipo + ".fbx";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(fbx) == null)
            {
                faltan.Add(fbx);
                continue;
            }

            try
            {
                AjustarImportador(fbx);
                Material mat = CrearMaterial(tipo);
                AnimatorController ctrl = CrearAnimator(tipo, fbx);
                GameObject prefab = CrearPrefab(tipo, fbx, mat, ctrl);
                AsignarEnDatos(tipo, prefab);
                listos.Add(tipo);
            }
            catch (System.Exception e)
            {
                Debug.LogError("ZPark: error configurando " + tipo + ": " + e.Message);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (listos.Count > 0)
            Debug.Log("ZPark: zombies 3D configurados: " + string.Join(", ", listos) +
                      ". Prefabs en Assets/Prefabs y asignados en Assets/Data.");
        foreach (string f in faltan)
            Debug.LogWarning("ZPark: no encontré " + f + ". Revisa que la carpeta y el nombre sean exactos.");
    }

    // 1. Importador del FBX
    static void AjustarImportador(string fbx)
    {
        ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
        imp.globalScale = 1f;
        imp.useFileScale = true;
        imp.importCameras = false;
        imp.importLights = false;
        imp.importAnimation = true;
        imp.animationType = ModelImporterAnimationType.Generic;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

        ModelImporterClipAnimation[] clips = imp.defaultClipAnimations;
        foreach (ModelImporterClipAnimation c in clips)
        {
            bool bucle = AnimsEnBucle.Contains(c.name);
            c.loopTime = bucle;
            c.loopPose = bucle;
        }
        imp.clipAnimations = clips;
        imp.SaveAndReimport();
    }

    // 2. Material URP con la textura de color
    static Material CrearMaterial(string tipo)
    {
        string ruta = Carpeta + "/" + tipo + "/" + tipo + "_Mat.mat";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Carpeta + "/" + tipo + "/" + tipo + "_Color.png");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            mat = new Material(sh);
            AssetDatabase.CreateAsset(mat, ruta);
        }

        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }
        else
        {
            Debug.LogWarning("ZPark: no encontré " + tipo + "_Color.png junto al FBX.");
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // 3. Animator Controller: un estado por animación (el código cambia entre ellas por nombre)
    static AnimatorController CrearAnimator(string tipo, string fbx)
    {
        string ruta = Carpeta + "/" + tipo + "/" + tipo + "_Animator.controller";

        // Se REUTILIZA el controlador si ya existe (borrarlo dejaba los prefabs sin controlador)
        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta);
        if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        foreach (ChildAnimatorState viejo in sm.states) sm.RemoveState(viejo.state);

        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(fbx)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .ToArray();

        AnimatorState inicial = null;
        foreach (AnimationClip clip in clips)
        {
            AnimatorState st = sm.AddState(clip.name);
            st.motion = clip;
            if (clip.name == "Caminar") inicial = st;
        }
        if (inicial != null) sm.defaultState = inicial;

        if (clips.Length == 0) Debug.LogWarning("ZPark: " + tipo + ".fbx no trae animaciones.");
        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    // 4. Prefab: raíz con collider + Zombie, y el modelo animado como hijo
    static GameObject CrearPrefab(string tipo, string fbx, Material mat, AnimatorController ctrl)
    {
        GameObject raiz = new GameObject("Zombie_" + tipo);

        CapsuleCollider col = raiz.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.875f, 0f);
        col.height = 1.75f;
        col.radius = 0.3f;

        raiz.AddComponent<Zombie>();

        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
        modelo.transform.SetParent(raiz.transform, false);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.Euler(0f, GiroModelo, 0f);

        // Quita colliders que pudiera traer el modelo (el bueno es el de la raíz)
        foreach (Collider c in modelo.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

        foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }

        Animator anim = modelo.GetComponent<Animator>();
        if (anim == null) anim = modelo.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        string ruta = "Assets/Prefabs/Zombie_" + tipo + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);

        // Comprobación: el Animator del prefab guardado debe tener su controlador
        GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
        Animator a = contenido.GetComponentInChildren<Animator>(true);
        if (a != null && a.runtimeAnimatorController == null)
        {
            a.runtimeAnimatorController = ctrl;
            PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
        }
        bool ok = a != null && a.runtimeAnimatorController != null;
        PrefabUtility.UnloadPrefabContents(contenido);
        if (!ok) Debug.LogError("ZPark: Zombie_" + tipo + " quedó SIN controlador de animación.");

        return AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
    }

    // 5. ZombieData.modelo
    static void AsignarEnDatos(string tipo, GameObject prefab)
    {
        ZombieData datos = AssetDatabase.LoadAssetAtPath<ZombieData>("Assets/Data/" + tipo + ".asset");
        if (datos == null)
        {
            Debug.LogWarning("ZPark: no existe Assets/Data/" + tipo + ".asset. Corre primero ZPark > Crear tipos de zombie.");
            return;
        }
        datos.modelo = prefab;
        EditorUtility.SetDirty(datos);
    }
}
#endif
