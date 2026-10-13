using UnityEngine;

public enum OutfitHat { Beanie, Cap, Beret, Bucket }

[System.Serializable]
public class OutfitLook
{
    public string label = "Look";

    [Header("Colours (one of each is picked per NPC)")]
    public Color[] topColors;
    [Tooltip("Cuffs, hems, collars and colour-blocking.")]
    public Color[] trimColors;
    public Color[] bottomColors;
    public Color[] shoeColors;
    public Color[] hatColors;
    [Tooltip("Bags, scarves, glasses, headphones and paint splatters.")]
    public Color[] accentColors;
    public Color soleColor = Color.white;

    [Header("Top")]
    [Tooltip("How much bigger than the body the jacket is. Raise if the original clothes poke through.")]
    [Range(1f, 1.5f)] public float topLoose = 1.1f;
    [Tooltip("How far the hem hangs below the hips (1 = down to the knee).")]
    [Range(0f, 1f)] public float hemDrop = 0.1f;
    [Tooltip("1 = full sleeves. Lower values expose the NPC's ORIGINAL arms/sleeves.")]
    [Range(0f, 1f)] public float sleeveLength = 1f;
    public bool quilted;
    public bool hood;
    public bool collar;
    [Range(0f, 1f)] public float colorBlockChance = 0f;

    [Header("Trousers")]
    [Tooltip("1 = full length. Lower values expose the NPC's ORIGINAL legs.")]
    [Range(0f, 1f)] public float pantsLength = 1f;
    [Range(1f, 1.5f)] public float pantsLoose = 1.1f;
    [Tooltip("1 = straight, 1.4 = wide leg.")]
    [Range(0.8f, 1.8f)] public float pantsFlare = 1f;
    public bool cargoPockets;

    [Header("Extras (chances 0-1)")]
    [Range(0f, 1f)] public float hatChance = 0.5f;
    public OutfitHat[] hatTypes = { OutfitHat.Beanie, OutfitHat.Cap };
    [Range(0f, 1f)] public float backpackChance = 0.3f;
    [Range(0f, 1f)] public float sashBagChance = 0.2f;
    [Range(0f, 1f)] public float headphonesChance = 0.2f;
    [Range(0f, 1f)] public float sunglassesChance = 0.2f;
    [Range(0f, 1f)] public float scarfChance = 0.2f;
    public int splatterMin = 0;
    public int splatterMax = 0;

    private static Color C(float r, float g, float b) { return new Color(r, g, b, 1f); }

    // ---------- Level NOT completed ----------
    public static OutfitLook Overcast()
    {
        return new OutfitLook
        {
            label = "Overcast (before)",
            topColors = new[] { C(0.16f, 0.17f, 0.19f), C(0.27f, 0.30f, 0.34f), C(0.30f, 0.32f, 0.26f), C(0.12f, 0.16f, 0.26f), C(0.45f, 0.46f, 0.47f) },
            trimColors = new[] { C(0.07f, 0.07f, 0.08f), C(0.45f, 0.28f, 0.20f), C(0.15f, 0.30f, 0.30f) },
            bottomColors = new[] { C(0.08f, 0.08f, 0.09f), C(0.22f, 0.23f, 0.25f), C(0.15f, 0.20f, 0.30f), C(0.42f, 0.38f, 0.30f) },
            shoeColors = new[] { C(0.08f, 0.08f, 0.09f), C(0.20f, 0.21f, 0.23f), C(0.32f, 0.33f, 0.35f) },
            hatColors = new[] { C(0.14f, 0.15f, 0.17f), C(0.12f, 0.16f, 0.26f), C(0.30f, 0.32f, 0.26f) },
            accentColors = new[] { C(0.55f, 0.35f, 0.18f), C(0.20f, 0.22f, 0.25f), C(0.35f, 0.40f, 0.42f) },
            soleColor = C(0.55f, 0.55f, 0.56f),
            topLoose = 1.16f,
            hemDrop = 0.35f,
            sleeveLength = 1f,
            quilted = true,
            hood = true,
            collar = false,
            colorBlockChance = 0.2f,
            pantsLength = 1f,
            pantsLoose = 1.10f,
            pantsFlare = 1f,
            cargoPockets = true,
            hatChance = 0.55f,
            hatTypes = new[] { OutfitHat.Beanie, OutfitHat.Cap },
            backpackChance = 0.5f,
            sashBagChance = 0.1f,
            headphonesChance = 0.3f,
            sunglassesChance = 0.05f,
            scarfChance = 0.35f,
            splatterMin = 0,
            splatterMax = 0
        };
    }

    // ---------- Level completed ----------
    public static OutfitLook Vibrant()
    {
        return new OutfitLook
        {
            label = "Vibrant (after)",
            topColors = new[] { C(1.00f, 0.42f, 0.38f), C(1.00f, 0.82f, 0.20f), C(0.10f, 0.70f, 0.70f), C(0.58f, 0.36f, 0.90f), C(0.55f, 0.85f, 0.25f), C(1.00f, 0.35f, 0.65f), C(0.30f, 0.60f, 1.00f) },
            trimColors = new[] { C(1f, 1f, 1f), C(0.10f, 0.12f, 0.30f), C(0.90f, 0.65f, 0.10f) },
            bottomColors = new[] { C(0.95f, 0.92f, 0.82f), C(0.15f, 0.30f, 0.85f), C(1.00f, 0.55f, 0.15f), C(0.50f, 0.90f, 0.70f), C(0.40f, 0.15f, 0.40f) },
            shoeColors = new[] { C(1f, 1f, 1f), C(0.90f, 0.15f, 0.15f), C(1.00f, 0.85f, 0.15f), C(0.50f, 0.90f, 0.70f) },
            hatColors = new[] { C(0.85f, 0.10f, 0.15f), C(0.10f, 0.65f, 0.65f), C(0.90f, 0.65f, 0.10f), C(1.00f, 0.45f, 0.70f) },
            accentColors = new[] { C(1.00f, 0.30f, 0.30f), C(0.20f, 0.80f, 0.90f), C(1.00f, 0.85f, 0.10f), C(0.65f, 0.40f, 1.00f), C(0.35f, 0.90f, 0.40f), C(1.00f, 0.50f, 0.80f) },
            soleColor = C(1f, 1f, 1f),
            topLoose = 1.07f,
            hemDrop = 0.08f,
            sleeveLength = 1f,
            quilted = false,
            hood = false,
            collar = true,
            colorBlockChance = 0.7f,
            pantsLength = 1f,
            pantsLoose = 1.12f,
            pantsFlare = 1.35f,
            cargoPockets = false,
            hatChance = 0.75f,
            hatTypes = new[] { OutfitHat.Beret, OutfitHat.Bucket, OutfitHat.Cap },
            backpackChance = 0.1f,
            sashBagChance = 0.5f,
            headphonesChance = 0.25f,
            sunglassesChance = 0.5f,
            scarfChance = 0.1f,
            splatterMin = 4,
            splatterMax = 9
        };
    }
}