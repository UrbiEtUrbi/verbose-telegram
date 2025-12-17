using UnityEngine;

public class InteractionChecker : MonoBehaviour
{


    IInteractible m_CurrentlyLookingAt;

    public IInteractible CurrentObject => m_CurrentlyLookingAt;

    void FixedUpdate()
    {

        
      Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out var HitInfo, 2f, LayerMask.GetMask("Interactible"));
        

        if(HitInfo.collider == null)
        {
            
            if(m_CurrentlyLookingAt != null)
            {
                m_CurrentlyLookingAt.StopLooking();
                m_CurrentlyLookingAt = null;
            }
            return;
        }
        var interactible = HitInfo.collider.gameObject.GetComponent<IInteractible>();
        if (interactible != null)
        {
            interactible.LookAt(); 
            if(m_CurrentlyLookingAt != null && m_CurrentlyLookingAt != interactible)
            {
                m_CurrentlyLookingAt.StopLooking();
            }
            m_CurrentlyLookingAt = interactible;
        }
        else
        {
            if(m_CurrentlyLookingAt != null)
            {
                m_CurrentlyLookingAt.StopLooking();
                m_CurrentlyLookingAt = null;
            }

        }
    
    }
    
}
