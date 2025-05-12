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

    private void Start()
    {
        localOrigin = transform.localPosition;
        worldspaceOrigin = transform.position;
        SetOffsets(minimumOffset, maximumOffset);
    }

    public void SetOffsets(Vector3 minimum, Vector3 maximum)
    {
        if (minimum.x > maximum.x)
        {
            float temp = minimum.x;
            minimum.x = maximum.x;
            maximum.x = temp;
        }
        if (minimum.y > maximum.y)
        {
            float temp = minimum.y;
            minimum.y = maximum.y;
            maximum.y = temp;
        }
        if (minimum.z > maximum.z)
        {
            float temp = minimum.z;
            minimum.z = maximum.z;
            maximum.z = temp;
        }
        minimumOffset = minimum;
        maximumOffset = maximum;
    }
}
