using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Clipper : System.Object
{
    // Each GameObject in the prefab must have a name that is unique to that prefab
    // because the GameObject names are used a keys in the materialSelectors list.
    
    // Note that this demo uses foreach which is not a good idea in a real project
    // since it could result in many unwanted allocations.

    // a unique list of all materials used by all renderers in the prefab
    private List<Material> modelMaterials;

    // a matching list of new materials using a cross-section shader
    private List<Material> clipMaterials;

    // key string is the name of a GameObject in this prefab which contains a Renderer.
    // the int array is the position in the modelMaterials and clipMaterials lists for
    // each Material referenced by the Renderer.
    private Dictionary<string, int[]> materialMap;

    public void Clip(bool apply, GameObject prefab, GameObject instance)
    {
        // if we haven't dealt with the materials in this prefab yet, do it once
        if (modelMaterials == null) PrepareMaterials(prefab);

        // grab the render list every time in case this is a different instance
        // in a real program this should be stored by the instance
        Renderer[] instanceRenderers = instance.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in instanceRenderers)
        {
            string key = r.gameObject.name;
            int[] idx = materialMap[key];
            Material[] updatedMaterials = new Material[idx.Length];
            for (int i = 0; i < idx.Length; i++)
            {
                updatedMaterials[i] = apply ? clipMaterials[idx[i]] : modelMaterials[idx[i]];
            }
            r.materials = updatedMaterials;
        }
    }

    public void SetClippingPlane(Vector4 SectionPoint, Vector4 SectionPlane, float SectionOffset)
    {
        for (int i = 0; i < clipMaterials.Count; i++)
        {
            clipMaterials[i].SetVector("_SectionPoint", SectionPoint);
            clipMaterials[i].SetVector("_SectionPlane", SectionPlane);
            clipMaterials[i].SetFloat("_SectionOffset", SectionOffset);
        }
    }

    private void PrepareMaterials(GameObject prefab)
    {
        Renderer[] prefabRenderers = prefab.GetComponentsInChildren<Renderer>();

        // By using a GameObject to mark the clip effect location,
        // there is no need to find an arbitrary location such as
        // the model center. This would still be useful to locate
        // the extents of a regularly-shaped model.
        //Bounds bound = prefabRenderers[0].bounds;
        //for (int i = 1; i < prefabRenderers.Length; i++)
        //{
        //    bound.Encapsulate(prefabRenderers[i].bounds);
        //}
        //center = bound.center;
        //maximumOffset = bound.extents.magnitude;

        modelMaterials = new List<Material>();
        clipMaterials = new List<Material>();
        materialMap = new Dictionary<string, int[]>();

        // get references to each unique material in the model
        foreach (Renderer r in prefabRenderers)
        {
            string key = r.gameObject.name;
            Material[] m = r.sharedMaterials;
            int[] idx = new int[m.Length];
            for (int j = 0; j < m.Length; j++)
            {
                int i = modelMaterials.IndexOf(m[j]);
                if (i == -1)
                {
                    modelMaterials.Add(m[j]);
                    i = modelMaterials.Count - 1;
                }
                idx[j] = i;
            }
            // each GameObject in the prefab must have a name that is unique to that prefab
            materialMap.Add(key, idx);
        }

        // generate a clip version of each material
        foreach (Material m in modelMaterials)
        {
            Material clipMat = new Material(m);
            string shaderName = m.shader.name;
            shaderName = shaderName.Replace("Legacy Shaders/", "");
            Shader clipShader = null;
            #if UNITY_WEBGL
                if (shaderName == "Standard") replacementShader = Shader.Find("CrossSection/Reflective/Specular");
            #endif
            if (clipShader == null) clipShader = Shader.Find("CrossSection/" + shaderName);
            if (clipShader == null)
            {
                if (shaderName.Contains("Transparent/VertexLit"))
                {
                    clipShader = Shader.Find("CrossSection/Transparent/Specular");
                }
                else if (shaderName.Contains("Transparent"))
                {
                    clipShader = Shader.Find("CrossSection/Transparent/Diffuse");
                }
                else
                {
                    clipShader = Shader.Find("CrossSection/Diffuse");
                }
            }
            clipMat.name = "CrossSection " + m.name;
            clipMat.shader = clipShader;
            clipMat.EnableKeyword("CLIP_ONE");
            clipMat.SetVector("_SectionPoint", Vector4.zero);
            clipMat.SetVector("_SectionPlane", Vector4.zero);
            clipMat.SetFloat("_SectionOffset", 0f);
            //clipMat.SetColor("_SectionColor", m.color);
            clipMat.SetColor("_SectionColor", Color.black);
            //clipMat.color = Color.cyan; // debug/test: make it obvious when the clip material is active
            clipMaterials.Add(clipMat);
        }
    }

}

