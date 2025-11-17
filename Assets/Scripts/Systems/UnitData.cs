using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Unit", menuName = "UnitData", order = 1)]
public class UnitData : ScriptableObject
{
    public int ID;
    public string Name;
    public Sprite Image;
    public float Scale;
    public float Mass;
    public int Score;

#if UNITY_EDITOR
    private void OnValidate()
    {
        var sprites = Resources.LoadAll<Sprite>("Images/Planets");
        var used = new System.Collections.Generic.HashSet<string>();
        foreach (var g in AssetDatabase.FindAssets("t:UnitData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<UnitData>(AssetDatabase.GUIDToAssetPath(g));
            if (d != null && d != this && d.Image != null)
                used.Add(d.Image.name);
        }

        Sprite pick = null;
        if (Image == null || used.Contains(Image.name))
        {
            foreach (var s in sprites)
            {
                if (!used.Contains(s.name)) { pick = s; break; }
            }
            Image = pick;
        }

        if (Image != null)
        {
            var m = Regex.Match(Image.name, @"^(?<num>\d+)\.");
            ID = m.Success ? int.Parse(m.Groups["num"].Value) : ID;
        }
        else ID = 0;

        if (Image != null)
        {
            string rawName = Image.name;
            Name = Regex.Replace(rawName, @"^\d+\.", "");
        }
        else Name = null;

        Scale = 0.5f + (float)ID * 0.35f;
        Mass = ID;
        Score = ID * (ID + 1) / 2;
    }
#endif

    public UnitData Clone()
    {
        UnitData clone = ScriptableObject.CreateInstance<UnitData>();

        clone.ID = this.ID;
        clone.Name = this.Name;
        clone.Image = this.Image;
        clone.Scale = this.Scale;
        clone.Mass = this.Mass;
        clone.Score = this.Score;

        return clone;
    }
}
