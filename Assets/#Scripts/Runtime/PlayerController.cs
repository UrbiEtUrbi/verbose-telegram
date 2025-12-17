using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{


    InteractionChecker m_InteractionChecker;

    void Awake()
    {
        m_InteractionChecker = GetComponentInChildren<InteractionChecker>();
    }


    public void Interact(InputAction.CallbackContext context)
    {
        Debug.Log(context.started);
       if(context.started && m_InteractionChecker.CurrentObject != null){
            
            m_InteractionChecker.CurrentObject.Interact();
        }
    }
}
