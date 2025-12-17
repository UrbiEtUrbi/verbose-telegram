using UnityEngine;


[RequireComponent(typeof(PickupView))]
public class Pickup : MonoBehaviour, IInteractible
{
   [SerializeField]
   PickupDefinition pickupDefinition;

   public PickupDefinition PickupDef => pickupDefinition;


   PickupView m_PickupView;




    void Awake()
    {
        m_PickupView = GetComponent<PickupView>();
        m_PickupView.Init();
    }

    public void LookAt()
    {
        m_PickupView.ToggleOutline(true);
    }

    public void StopLooking()
    {
        m_PickupView.ToggleOutline(false);
    }

    public void Interact()
    {
        Destroy(gameObject);
    }
}
