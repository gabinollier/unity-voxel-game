using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] TextMeshProUGUI amountText;
    [SerializeField] Image itemIcon;
    [SerializeField] RectTransform visual;
    [SerializeField] GameObject emptyBorder, communBorder, rareBorder, legendaryBorder;
    Transform _parentWhenDragging;

    public int IndexInInventory { get; private set; }
    bool _hasAnItem = false;
    bool _isDragging = false;

    public void SetParentUsedWhenDragging(Transform parentWhenDragging)
    {
        _parentWhenDragging = parentWhenDragging;
    }

    public void ChangeItem(ItemStack? stack, int indexInInventory)
    {
        _hasAnItem = stack.HasValue;
        IndexInInventory = indexInInventory;

        if (!_hasAnItem)
        {
            amountText.text = "";
            itemIcon.color = Color.clear;
            emptyBorder.SetActive(true);
            communBorder.SetActive(false);
            rareBorder.SetActive(false);
            legendaryBorder.SetActive(false);
            return;
        }

        itemIcon.sprite = stack.Value.ItemData.Icon;
        itemIcon.color = Color.white;

        emptyBorder.SetActive(false);
        communBorder.SetActive(stack.Value.ItemData.Rarity == ItemRarity.Common);
        rareBorder.SetActive(stack.Value.ItemData.Rarity == ItemRarity.Rare);
        legendaryBorder.SetActive(stack.Value.ItemData.Rarity == ItemRarity.Legendary);

        if (stack.Value.ItemData.MaxStackSize == 1)
            amountText.text = "";
        else
            amountText.text = stack.Value.Amount.ToString();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_hasAnItem && eventData.button == PointerEventData.InputButton.Left)
        {
            visual.SetParent(_parentWhenDragging);
            visual.SetAsLastSibling();
            _isDragging = true;
        }
    }

    private void Update()
    {
        if (_isDragging)
        {
            visual.position = Vector3.Lerp(visual.position, Mouse.current.position.ReadValue(), 25 * Time.deltaTime);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_isDragging)
        {
            _isDragging = false;

            InventorySlot inventorySlot = null;

            foreach (GameObject go in eventData.hovered)
            {
                if (go.TryGetComponent(out inventorySlot))
                    break;
            }

            if (inventorySlot != null && inventorySlot.IndexInInventory != IndexInInventory)
                InventoryManager.Swap(inventorySlot.IndexInInventory, IndexInInventory);


            visual.SetParent(transform);
            visual.localPosition = Vector3.zero;
        }
    }

    public void OnHover()
    {
    }

    public void OnExit()
    {
    }
}
