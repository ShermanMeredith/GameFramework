using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

public class MyFlatGroups : MonoBehaviour {
    public int numElement = 10;

	void Start () {
        GetComponent<PTFlatGroups>().CreateElements(numElement);
	}
}
