using UnityEngine;

public interface IEquippable
{
    void Equip(GameObject user);
    void Unequip(GameObject user);
}