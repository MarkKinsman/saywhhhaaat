using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace MortVR
{
    public class SectionController : Object
    {
        #region Variables

        public Shader standardShader;
        public Shader sectionShader;
        public GameObject miniModel;
        List<Material> modelMaterials;

        #endregion

        public void SetSectionShader()
        {
            for (int i = 0; i < modelMaterials.Count; i++)
            {
                modelMaterials[i].shader = sectionShader;
                modelMaterials[i].EnableKeyword("CLIP_ONE");
                modelMaterials[i].SetColor("_SectionColor", Color.black);
            }
        }

        public void SetStandardShader()
        {
            for (int i = 0; i < modelMaterials.Count; i++)
            {
                modelMaterials[i].shader = standardShader;
            }
        }

        public void cutSection(Vector4 SectionPoint, Vector4 SectionPlane, float SectionOffset)
        {
            for (int i = 0; i < modelMaterials.Count; i++)
            {
                modelMaterials[i].SetVector("_SectionPoint", SectionPoint);
                modelMaterials[i].SetVector("_SectionPlane", SectionPlane);
                modelMaterials[i].SetFloat("_SectionOffset", SectionOffset);
            }
        }

        public void getMaterials()
        {
            modelMaterials = new List<Material>();

            Renderer[] prefabRenderers = miniModel.GetComponentsInChildren<Renderer>();

            foreach (Renderer r in prefabRenderers)
            {
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
            }
        }
    }
}


