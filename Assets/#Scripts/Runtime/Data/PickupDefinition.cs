using UnityEngine;


[CreateAssetMenu(fileName = "PickupDefinition", menuName = "Pickups/Pickup Definition")]
public class PickupDefinition : ScriptableObject
{
    [SerializeField] private string pickupName;
    [SerializeField] private Sprite pickupIcon;

    public string PickupName => pickupName;
    public Sprite PickupIcon => pickupIcon;
}
