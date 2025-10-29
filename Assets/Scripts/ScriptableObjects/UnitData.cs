using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(fileName = "Unit", menuName = "UnitData", order = 1)]
public class UnitData : ScriptableObject
{
    public int unitID;
    public string unitName;
    public Sprite unitImage;
    public float unitScale;
    public float unitMass;
    public int unitScore;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (unitImage != null)
        {
            string rawName = unitImage.name;
            unitName = Regex.Replace(rawName, @"^\d+\.", "");
        }

        unitScale = 0.5f + (float)unitID * 0.35f;
        unitMass = unitID;

        unitScore = unitID * (unitID + 1) / 2;
    }
#endif

    public UnitData Clone()
    {
        UnitData clone = ScriptableObject.CreateInstance<UnitData>();

        clone.unitID = this.unitID;
        clone.unitName = this.unitName;
        clone.unitImage = this.unitImage;

        clone.unitScale = this.unitScale;
        clone.unitMass = this.unitMass;

        clone.unitScore = this.unitScore;

        return clone;
    }
}
