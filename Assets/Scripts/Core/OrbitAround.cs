using UnityEngine;

public class OrbitAround : MonoBehaviour
{
    public Transform center;
    private float radius;
    private float angle;

    void Start()
    {
        if (center == null) return;
        radius = Vector3.Distance(center.position, transform.position);
        Vector3 dir = (transform.position - center.position).normalized;
        angle = Mathf.Atan2(dir.y, dir.x);
    }

    void Update()
    {
        if (center == null) return;

        // Pega o ShieldRotator no centro (se existir)
        ShieldRotator rotator = center.GetComponent<ShieldRotator>();
        if (rotator == null) return;

        float speed = rotator.rotationSpeed;
        angle += speed * Mathf.Deg2Rad * Time.deltaTime;

        // Calcula nova posição em volta do centro
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
        transform.position = center.position + offset;
    }
}
