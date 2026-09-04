using UnityEngine;


public class InteractIndicator : MonoBehaviour
{
    [Header("Look")]
    public float heightAboveHead = 2.2f;
    public float bobAmount = 0.15f;
    public float bobSpeed = 3f;
    public int fontSize = 48;
    public Color color = new Color(1f, 0.85f, 0.1f); 

    private Transform followTarget;
    private TextMesh textMesh;
    private Camera cam;
    private float bobTimer;

    
    public void Init(Transform target)
    {
        followTarget = target;
        cam = Camera.main;

        textMesh = gameObject.AddComponent<TextMesh>();
        textMesh.text = "!";
        textMesh.characterSize = 0.3f;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = color;

        bobTimer = Random.Range(0f, 10f); 
    }

    private void Update()
    {
        if (followTarget == null) return;
        if (cam == null) cam = Camera.main;

        bobTimer += Time.deltaTime * bobSpeed;
        float bob = Mathf.Sin(bobTimer) * bobAmount;

        transform.position = followTarget.position + Vector3.up * (heightAboveHead + bob);

        if (cam != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}