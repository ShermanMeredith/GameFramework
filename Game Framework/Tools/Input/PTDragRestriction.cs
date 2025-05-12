using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PTDragRestriction : MonoBehaviour
{
    public enum OffsetSpace { LocalSpace, WorldSpace}

    [SerializeField]
    private Vector3 minimumOffset, maximumOffset;
    private Vector3 localOrigin;
    private Vector3 worldspaceOrigin;

    public OffsetSpace offsetSpace;

    public Vector3 MinimumLocalPosition { get { return localOrigin + minimumOffset; } }
    public Vector3 MaximumLocalPosition { get { return localOrigin + maximumOffset; } }

    public Vector3 MinimumWorldPosition { get { return worldspaceOrigin + minimumOffset; } }
    public Vector3 MaximumWorldPosition { get { return worldspaceOrigin + maximumOffset; } }

    private void Awake()
    {
        localOrigin = transform.localPosition;
        worldspaceOrigin = transform.position;
    }

    public void SetOffsets(Vector3 minimum, Vector3 maximum)
    {
        minimumOffset = minimum;
        maximumOffset = maximum;
    }
}
