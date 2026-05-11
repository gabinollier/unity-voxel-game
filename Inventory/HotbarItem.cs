using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotbarItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI amountText;
    [SerializeField] Image icon;
    public void ChangeItem(ItemStack itemStack)
    {
        gameObject.SetActive(true);
        icon.sprite = itemStack.ItemData.Icon;
        amountText.text = itemStack.Amount.ToString();
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }
}
