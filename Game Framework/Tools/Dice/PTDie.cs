using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

public class PTDie : MonoBehaviour
{
    public Rigidbody rigidBody;
    public MeshRenderer meshRenderer;
    protected PTDieFace[] faces;
    private bool wasRolling;

    public float Width {  get { return meshRenderer.bounds.size.x; } }
    public bool IsRolling { get { return !rigidBody.IsSleeping(); } }
    public Vector3 GoBackPosition { get; set; }

    public int RollValue
    {
        get
        {
            PTDieFace highestFace = faces[0];
            foreach (PTDieFace face in faces)
            {
                if (face.transform.position.y > highestFace.transform.position.y)
                {
                    highestFace = face;
                }
            }
            return highestFace.MyValue;
        }
    }

    protected virtual void Awake()
    {
        faces = GetComponentsInChildren<PTDieFace>();
    }

    public IEnumerator Fade(bool visible, float timer)
    {
        float targetAlpha = visible ? 1 : 0;

        StartCoroutine(rigidBody.transform.SetAlphaCoroutine(targetAlpha, timer));
        foreach(PTDieFace face in faces)
        {
            StartCoroutine(face.transform.SetAlphaCoroutine(targetAlpha, timer));
        }
        yield return new WaitForSeconds(Time.deltaTime);
    }

    public void Roll(float power)
    {
        rigidBody.isKinematic = false;
        rigidBody.velocity = Vector3.zero;

        transform.rotation = Random.rotation;

        rigidBody.AddForce(transform.forward * power * 2, ForceMode.Impulse);
        rigidBody.AddTorque(Random.insideUnitSphere * power, ForceMode.Impulse);
        StartCoroutine(RigidbodyTracker());
    }

    public void Roll(float power, Vector3 target)
    {
        rigidBody.isKinematic = false;
        rigidBody.velocity = Vector3.zero;

        rigidBody.AddForce(Vector3.Normalize(target - rigidBody.transform.position) * power * 2, ForceMode.Impulse);
        rigidBody.AddTorque(Random.insideUnitSphere * power, ForceMode.Impulse);
    }

    IEnumerator RigidbodyTracker()
    {
        while(rigidBody.isKinematic == false && rigidBody.IsSleeping() == false)
        {
            Debug.LogError("Is Kinematic? " + rigidBody.isKinematic);
            Debug.LogError("Is Sleeping?? " + rigidBody.IsSleeping());

            yield return new WaitForSeconds(Time.deltaTime);
        }
        Debug.LogError("is kinematic and is sleeping");
    }

    public void Flip()
    {

    }

    public void Tip()
    {

    }

    public void Set(int value)
    {

    }

    public void Set(PTDieFace face)
    {

    }

    public void Hide()
    {

    }

    public void Spin(int numberOfSpins)
    {

    }
}
