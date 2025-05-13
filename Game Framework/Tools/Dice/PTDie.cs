using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayTable;

public class PTDie : MonoBehaviour
{
    public Rigidbody rigidBody;
    public MeshRenderer meshRenderer;
    
    protected PTDieFace[] faces;

    private float torqueTimer;
    private Vector3 torqueToAdd;

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

    public int FaceCount { get { return faces.Length; } }

    protected virtual void Awake()
    {
        faces = GetComponentsInChildren<PTDieFace>();
    }

    private void FixedUpdate()
    {
        if (torqueTimer > 0)
        {
            rigidBody.AddTorque(torqueToAdd);
            torqueTimer -= Time.deltaTime;
        }
    }

    public void SetAsYellowCatanDie()
    {
        rigidBody.GetComponent<Renderer>().material.color = new Color(Color.yellow.r * 0.8f, Color.yellow.g * 0.8f, 0);
        foreach (PTDieFace face in faces)
        {
            face.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0, 0);
        }
    }

    public IEnumerator Fade(bool visible, float timer)
    {
        float targetAlpha = visible ? 1 : 0;
        rigidBody.GetComponent<Collider>().enabled = visible;
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
        rigidBody.linearVelocity = Vector3.zero;

        transform.rotation = Random.rotation;

        rigidBody.AddForce(transform.forward * power * 2, ForceMode.Impulse);
        Vector3 torqueToAdd = Random.insideUnitSphere * power;
        rigidBody.AddTorque(torqueToAdd, ForceMode.Impulse);
        StartCoroutine(RigidbodyTracker());
    }

    public void Roll(float power, Vector3 target)
    {
        rigidBody.isKinematic = false;
        rigidBody.linearVelocity = Vector3.zero;

        rigidBody.AddForce(Vector3.Normalize(target - rigidBody.transform.position) * power * 2, ForceMode.Impulse);
        float randomXTorque = Random.Range(8f, 10f);
        float randomYTorque = Random.Range(8f, 10f);
        float randomZTorque = Random.Range(8f, 10f);
        torqueToAdd = new Vector3(randomXTorque, randomYTorque, randomZTorque);
        torqueTimer = 0.5f;
        //Debug.LogError(new Vector3(randomXTorque, randomYTorque, randomZTorque) * power * 1000);
        //rigidBody.AddTorque(new Vector3(randomXTorque, randomYTorque, randomZTorque) * power * 1000, ForceMode.Force);
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

    public void Set(int value, float timer = PT.DEFAULT_TIMER)
    {
        StartCoroutine(SetCoroutine(value, timer));
    }

    public IEnumerator SetCoroutine(int value, float timer = PT.DEFAULT_TIMER)
    {
        PTDieFace faceToSet = null;
        foreach (PTDieFace face in faces)
        {
            if (face.MyValue == value)
            {
                faceToSet = face;
                break;
            }
        }
        if (faceToSet != null)
        {
            yield return StartCoroutine(SetCoroutine(faceToSet, timer));
        }
        else
        {
            Debug.LogError("Trying to set die to face that does not exist: face = " + value);
        }
    }

    public void Set(PTDieFace face, float timer = PT.DEFAULT_TIMER)
    {
        StartCoroutine(SetCoroutine(face, timer));
    }

    public IEnumerator SetCoroutine(PTDieFace face, float timer = PT.DEFAULT_TIMER)
    {
        bool wasKinematic = rigidBody.isKinematic;
        rigidBody.isKinematic = true;
        yield return rigidBody.transform.SetWorldRotationCoroutine(Quaternion.FromToRotation(face.transform.localPosition, Vector3.up), timer);
        rigidBody.isKinematic = wasKinematic;
    }

    public void Hide()
    {

    }

    public void Spin(int numberOfSpins)
    {

    }
}
