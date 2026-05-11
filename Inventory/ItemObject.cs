using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ItemObject : MonoBehaviour
{
    [Header("Sphere Collider is the detection range. absorptionRadius is the radius where the item will be destroyed and obsorbed by the player.")]
    [Space]
    [SerializeField] private float obsorptionRadius;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private Vector3 playerPositionOffset;
    [SerializeField] private float pickupForce;
    [SerializeField] private float activatePickupAfter;
    [SerializeField] private string sound;
    [Space]
    [SerializeField] private SpriteRenderer visual;
    [Space]
    [SerializeField] private bool drawGizmos;

    private bool isBeingPickedUp = false;
    private bool isPickupActivated = false;
    private Rigidbody rb;
    private Vector3 velocityReference = Vector3.zero;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Invoke("Activate", activatePickupAfter);
    }

    private void Activate()
    {
        isPickupActivated = true;
        if (isBeingPickedUp) rb.useGravity = false;
    }

    public void Pickup()
    {
        isBeingPickedUp = true;
        if (isPickupActivated) rb.useGravity = false;
    }

    private void FixedUpdate()
    {
        if (!isBeingPickedUp) return;
        if (!isPickupActivated) return;

        //rb.velocity = (SceneReferences.Instance.player.transform.position + playerPositionOffset - rb.position).normalized * pickupForce;
        if (Physics.CheckSphere(transform.position, obsorptionRadius, playerMask, QueryTriggerInteraction.Collide))
        {
            //InventoryManager.TryToAdd(data);
            isBeingPickedUp = false;
            rb.useGravity = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, obsorptionRadius);
    }

}
