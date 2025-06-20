using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public float interactionRange = 2f; // How close the player must be to pick up items
    public LayerMask itemLayer;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) // Press E to pick up items
        {
            TryPickUpItem();
        }
    }

    void TryPickUpItem()
    {
        Collider[] items = Physics.OverlapSphere(transform.position, interactionRange, itemLayer);

        foreach (Collider itemCollider in items)
        {
            Item item = itemCollider.GetComponent<Item>();
            if (item != null && item.isCollectible)
            {
                FindObjectOfType<UIManager>().AddScore(item.scoreValue); // Update score in UI
                item.PickUp();
                return;
            }
        }
    }
}
