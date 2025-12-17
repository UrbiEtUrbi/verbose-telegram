using UnityEngine;


[RequireComponent(typeof(MeshRenderer))]
public class PickupView : MonoBehaviour
{


    MeshRenderer m_MeshRenderer;

    Material m_Material;
    
   public void Init()
    {
        m_MeshRenderer = GetComponent<MeshRenderer>();
        m_Material = new Material(m_MeshRenderer.sharedMaterial);
        m_MeshRenderer.sharedMaterial = m_Material;
        ToggleOutline(false);

    }


    public void ToggleOutline(bool active)
    {
        
        m_Material.SetFloat("_OutlineWidth", active ? 0.02f : 0);
    }
}
