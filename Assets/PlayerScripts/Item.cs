using UnityEngine;

public class Item : MonoBehaviour
{
    public string itemName; // Name of the item
    public bool isCollectible = true; // If the item can be picked up
    public int scoreValue = 10; // Score amount this item gives when picked up

    public void PickUp()
    {
        Debug.Log("Picked up: " + itemName + " | Score: " + scoreValue);
        Destroy(gameObject); // Destroy item no inventory needed
    }
}
