using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

public class PTLineupZone : PTLayoutZone_new
{
    // Start is called before the first frame update
    void Start()
    {
        OnAdded += OnAddedHandler;
        OnRemoved += (obj) => { StartCoroutine(WaitAndSetRelativePositions()); };
        OnArranged += () => SetContentsInteractive(true);

        Arrange();
    }

    private void OnAddedHandler(Transform obj, Transform from)
    {
        PTSeatingSeat fromSeat = from.GetComponentInParent<PTSeatingSeat>();
        if (fromSeat != null)
        {
            obj.GetComponent<PTSeatingIcon>().OnPlacedInLineup();
            if (fromSeat.Zone.Count == 0)
            {
                fromSeat.ResetSeat();
            }
        }
        if (obj.GetComponentInParent<PTSeatingSeat>() == null && obj.GetComponent<PTSeatingIcon>().IsDispensed)
        {
            Destroy(obj.gameObject);
        }
        obj.GetComponent<Collider>().enabled = true;
    }

    private IEnumerator WaitAndSetRelativePositions()
    {
        yield return new WaitForSeconds(PT.DEFAULT_TIMER);
        SetRelativePositions();
    }

    public void SetRelativePositions()
    {
        PTSeatingIcon[] icons = GetComponentsInChildren<PTSeatingIcon>();
        System.Array.Sort(icons, (a, b) => (a.SiblingIndex.CompareTo(b.SiblingIndex)));
        for (int i = 0; i < icons.Length; ++i)
        {
            icons[i].transform.SetSiblingIndex(i);
        }
        Arrange(PT.DEFAULT_TIMER / 2);
    }
}
