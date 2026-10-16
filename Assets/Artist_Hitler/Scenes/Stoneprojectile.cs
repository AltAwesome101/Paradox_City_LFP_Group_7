using UnityEngine;

/// <summary>
/// Added automatically to thrown stones by ArtistLevelPlayer.
/// When the stone hits something it makes a noise that IHearsNoise listeners can react to.
/// </summary>
public class StoneProjectile : MonoBehaviour
{
    public float noiseRadius = 25f;
    public float minImpactSpeed = 1.5f;
    public int maxNoiseEvents = 3;
    public float lifetime = 20f;
    public AudioClip impactClip;
    public float impactVolume = 1f;

    int noiseCount;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (noiseCount >= maxNoiseEvents)
            return;

        float speed = collision.relativeVelocity.magnitude;
        if (speed < minImpactSpeed)
            return;

        noiseCount++;

        // Harder impacts are louder (50% - 100% of the full radius)
        float loudness = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(speed / 8f));
        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

        if (impactClip != null)
            AudioSource.PlayClipAtPoint(impactClip, point, impactVolume);

        NoiseSystem.Emit(point, noiseRadius * loudness);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
}