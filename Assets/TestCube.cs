using System.Collections;
using UnityEngine;

public class TestCube : MonoBehaviour
{
    [Header("Spheres")]
    [SerializeField] private Transform RedSphare_transform;
    [SerializeField] private Transform PinkSphare_transform;
    [SerializeField] private Transform YellowSphare_transform;

    [Header("Cubes")]
    [SerializeField] private Transform GreenCube_transform;
    [SerializeField] private Transform BlueCube_transform;
    [SerializeField] private Transform SkyBlueCube_transform;

    [Header("Cylinder Object")]
    [SerializeField] private GameObject Sillender_transform;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 50f;
    [SerializeField] private float rotationDuration = 2f;
    [SerializeField] private float offsetDistance = 5f;

    private void Start()
    {
       // StartCoroutine(Progress());
        Vector3.Lerp( transform.position,RedSphare_transform.position, 5f);
    }

    private IEnumerator Progress()
    {
        // List of all targets in order
        Transform[] targets = {
            GreenCube_transform,
            RedSphare_transform,
            BlueCube_transform,
            PinkSphare_transform,
            YellowSphare_transform,
            SkyBlueCube_transform
        };

        foreach (Transform target in targets)
        {
            yield return MoveToTarget(target);
            yield return RotateAroundTarget(target);
        }
    }

    private IEnumerator MoveToTarget(Transform target)
    {
        Vector3 targetPosition = target.position + new Vector3(offsetDistance, 0, 0); // offset of 10 on X-axis
        while (Vector3.Distance(Sillender_transform.transform.position, targetPosition) > 0.1f)
        {
            Sillender_transform.transform.position = Vector3.MoveTowards(
                Sillender_transform.transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }
    }

    private IEnumerator RotateAroundTarget(Transform target)
    {
        float timer = 0f;
        while (timer < rotationDuration)
        {
            Sillender_transform.transform.RotateAround(
                target.position,
                Vector3.up, // rotate around Y-axis for 3D
                rotateSpeed * Time.deltaTime
            );
            timer += Time.deltaTime;
            yield return null;
        }
    }
}
