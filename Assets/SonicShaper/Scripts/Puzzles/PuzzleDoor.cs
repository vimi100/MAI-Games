using UnityEngine;
using System.Collections;

public class PuzzleDoor : MonoBehaviour
{
    public enum DoorType { Slide, Rotate }

    [Header("Door Config")]
    public DoorType doorType = DoorType.Slide;
    public Vector3 slideOffset = new Vector3(0, 3f, 0);
    public float rotationAngle = 90f;
    public Vector3 rotationAxis = Vector3.up;
    public float openDuration = 1f;

    [Header("Audio")]
    public AudioClip openClip;

    private Vector3 _closedPosition;
    private Quaternion _closedRotation;
    private bool _isOpen = false;
    private AudioSource _audioSource;

    void Awake()
    {
        _closedPosition = transform.position;
        _closedRotation = transform.rotation;
        _audioSource = GetComponent<AudioSource>();
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (_audioSource != null && openClip != null)
            _audioSource.PlayOneShot(openClip);

        if (doorType == DoorType.Slide)
            StartCoroutine(MoveTo(transform.position, _closedPosition + slideOffset, openDuration));
        else
            StartCoroutine(RotateTo(transform.rotation, _closedRotation * Quaternion.AngleAxis(rotationAngle, rotationAxis), openDuration));
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (doorType == DoorType.Slide)
            StartCoroutine(MoveTo(transform.position, _closedPosition, openDuration));
        else
            StartCoroutine(RotateTo(transform.rotation, _closedRotation, openDuration));
    }

    IEnumerator MoveTo(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        transform.position = to;
    }

    IEnumerator RotateTo(Quaternion from, Quaternion to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        transform.rotation = to;
    }
}
