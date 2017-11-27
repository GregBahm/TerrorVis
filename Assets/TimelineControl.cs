using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimelineControl : MonoBehaviour
{
    public MainScript Main;

    public Material YearBoxMat;
    private const int startYear = 1970;
    private const int endYear = 2017;

    private List<Transform> _yearBoxes;

    public float SelectedScale;
    [Range(.7f, 1f)]
    public float BoxMargin;
    private Vector3 _selectedScale;
    private Vector3 _baseScale;

    // Use this for initialization
    void Start () 
    {
        _yearBoxes = new List<Transform>();
        int years = endYear - startYear;
        float halfOffset = (float)(years - 1) / 2;
        for (int i = 0; i < years; i++)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = (startYear + i).ToString();
            obj.transform.position = new Vector3(0, 0, i - halfOffset);
            obj.transform.SetParent(this.transform, false);
            obj.GetComponent<MeshRenderer>().sharedMaterial = YearBoxMat;
            _yearBoxes.Add(obj.transform);
        }

	}
	
	// Update is called once per frame
	void Update ()
    {
        _baseScale = new Vector3(BoxMargin, BoxMargin, BoxMargin);
        _selectedScale = new Vector3(SelectedScale, SelectedScale, BoxMargin);
        for (int i = 0; i < _yearBoxes.Count; i++)
        {
            DoYearBox(i);
        }
	}

    private void DoYearBox(int i)
    {
        float param = (float)i / _yearBoxes.Count;
        float distToTime = Mathf.Abs(Main.Time - param);

        float weight = distToTime / Main.Range;
        weight = Mathf.Clamp01(weight);
        weight = Mathf.Pow(weight, 2);

        float fill = Mathf.Clamp01((Main.Range - 1f) * 2);
        weight = Mathf.Lerp(weight, 0, fill);

        Transform box = _yearBoxes[i];
        box.localScale = Vector3.Lerp( _selectedScale, _baseScale, weight);
        box.localPosition = new Vector3(0, box.localScale.y / 2, box.localPosition.z);
    }
}
