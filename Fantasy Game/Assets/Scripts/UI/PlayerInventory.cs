using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using LightPat.Core;
using LightPat.Core.Player;
using TMPro;
using UnityEngine.UI;

namespace LightPat.UI
{
    public class PlayerInventory : Menu
    {
        public GameObject weaponSlotButton;
        public Vector3 buttonStartingPosition;
        public float buttonSpacing;
        GameObject[] weaponSlotButtons;

        WeaponLoadout weaponLoadout;
        int fromIndex = -1;
        Image selectedButton;
        Color originalColor;

        public void ChangeWeaponLoadout(int slotIndex)
        {
            if (fromIndex == -1) // If we haven't selected our first slot yet
            {
                fromIndex = slotIndex;
                selectedButton = EventSystem.current.currentSelectedGameObject.GetComponent<Image>();
                originalColor = selectedButton.color;
                selectedButton.color = Color.gray;
            }
            else // We are selecting our second slot (the slot that our first choice goes to)
            {
                weaponLoadout.ChangeLoadoutPositions(fromIndex, slotIndex);
                selectedButton.color = originalColor;
                UpdateWeaponSlotsButtonText();
                fromIndex = -1;
            }
        }

        private void Start()
        {
            weaponLoadout = GetComponentInParent<WeaponLoadout>();

            weaponSlotButtons = new GameObject[weaponLoadout.GetWeaponListLength()];
            for (int i = 0; i < weaponLoadout.GetWeaponListLength(); i++)
            {
                weaponSlotButtons[i] = Instantiate(weaponSlotButton, transform);
                weaponSlotButtons[i].transform.localPosition = buttonStartingPosition + new Vector3(0, i * buttonSpacing, 0);
                int x = i;
                weaponSlotButtons[i].GetComponent<Button>().onClick.AddListener(delegate { ChangeWeaponLoadout(x); });
            }

            UpdateWeaponSlotsButtonText();
        }

        private void UpdateWeaponSlotsButtonText()
        {
            for (int i = 0; i < weaponLoadout.GetWeaponListLength(); i++)
            {
                weaponSlotButtons[i].GetComponentInChildren<TextMeshProUGUI>().SetText(weaponLoadout.GetWeapon(i).name);
            }
        }
    }
}