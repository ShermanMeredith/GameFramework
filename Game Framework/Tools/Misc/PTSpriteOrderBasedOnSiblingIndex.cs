using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SortingLayer))]
public class PTSpriteOrderBasedOnSiblingIndex : MonoBehaviour
{
    [SerializeField]
    private Transform content;
    [SerializeField]
    private bool firstChildFront;

    private int prevContent;

	// Use this for initialization
	void Start ()
    {
		foreach (Transform child in content)
        {
            child.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder = firstChildFront ? content.childCount - child.GetSiblingIndex() - 1 : child.GetSiblingIndex();
        }
	}

    private void Update()
    {
        if(prevContent != content.childCount)
        {
            prevContent = content.childCount;
            foreach (Transform child in content)
            {
                if (child.GetComponent<UnityEngine.Rendering.SortingGroup>())
                {
                    child.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder = firstChildFront ? content.childCount - child.GetSiblingIndex() - 1 : child.GetSiblingIndex();
                }
                else if (child.GetComponent<SpriteRenderer>())
                {
                    child.GetComponent<SpriteRenderer>().sortingLayerName = "PlayerMat";
                    child.GetComponent<SpriteRenderer>().sortingOrder = firstChildFront ? content.childCount - child.GetSiblingIndex() - 1 : child.GetSiblingIndex();
                }
            }
        }
    }
}
