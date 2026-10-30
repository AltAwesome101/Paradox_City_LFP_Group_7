using System;
using UnityEngine;


[RequireComponent(typeof(CarController))]
public class CarAudio : MonoBehaviour
{
    

    [Header("===== MIX =====")]

    [Range(0f, 1f)] public float masterVolume = 0.8f;

    [Tooltip("Continuous layers: motor, hover drone, wind, tyre squeal, reverse beeper, charge-up whine.")]
    [Range(0f, 1f)] public float engineVolume = 0.6f;

    [Tooltip("One-shot effects: power-up/down, take-off, landing, hyper turn, impacts.")]
    [Range(0f, 1f)] public float effectsVolume = 0.9f;

    [Tooltip("How loud the motor hum is while the car is on but standing still.")]
    [Range(0f, 1f)] public float idleHumLevel = 0.6f;

    [Tooltip("Loudness of the tyre squeal (wheelspin, hard braking, hard cornering).")]
    [Range(0f, 1f)] public float tireSquealLevel = 0.8f;


    [Header("===== 3D SOUND =====")]

    [Tooltip("0 = flat 2D sound, 1 = fully positional (gets quieter as you walk away from the car).")]
    [Range(0f, 1f)] public float spatialBlend = 1f;

    public float minDistance = 6f;
    public float maxDistance = 90f;


    [Header("===== COLLISION SOUND =====")]

    [Tooltip("Impacts slower than this (m/s along the contact normal) make no sound.")]
    public float impactSoundMinSpeed = 3f;

    public float impactSoundCooldown = 0.15f;


   

    private const int OneShotVoices = 6;
    private const double TwoPi = 6.283185307179586;

    private CarController car;
    private int sampleRate;

    private Synth synth;
    private AudioClip synthClip;
    private AudioSource engineSource;

    private AudioSource[] oneShots;
    private int nextVictim;

    private readonly System.Collections.Generic.List<AudioClip> generated =
        new System.Collections.Generic.List<AudioClip>();

    private AudioClip clipPowerUp, clipPowerDown;
    private AudioClip clipTakeoff, clipLandingStart, clipTouchdown;
    private AudioClip clipHyperTurn, clipLocked, clipImpact;

    private bool prevDriven, prevFlying, prevLanding, prevFlipping;
    private float lastImpactTime = -999f;


   

    private void Awake()
    {
        car = GetComponent<CarController>();

        sampleRate = AudioSettings.outputSampleRate;
        if (sampleRate <= 0)
            sampleRate = 48000;

        
        synth = new Synth(sampleRate);

        engineSource = CreateSource("CarAudio_Synth");
        synthClip = AudioClip.Create("CarSynth", sampleRate * 4, 1, sampleRate, true, synth.Fill, OnSynthSetPosition);
        engineSource.clip = synthClip;
        engineSource.loop = true;

        
        oneShots = new AudioSource[OneShotVoices];
        for (int i = 0; i < OneShotVoices; i++)
            oneShots[i] = CreateSource("CarAudio_OneShot" + i);

        
        clipPowerUp = BuildChime(true);
        clipPowerDown = BuildChime(false);
        clipTakeoff = BuildTakeoff();
        clipLandingStart = BuildLanding();
        clipTouchdown = BuildTouchdown();
        clipHyperTurn = BuildHyperTurn();
        clipLocked = BuildLocked();
        clipImpact = BuildImpact();
    }

    private void Start()
    {
        prevDriven = car.isBeingDriven;
        prevFlying = car.IsFlying;
        prevLanding = car.IsLanding;
        prevFlipping = car.IsFlipping;
    }

    private void OnEnable()
    {
        if (engineSource != null && !engineSource.isPlaying)
            engineSource.Play();
    }

    private void OnDisable()
    {
        if (engineSource != null)
            engineSource.Stop();
    }

    private void OnDestroy()
    {
        if (engineSource != null)
            engineSource.Stop();

        if (synthClip != null)
            Destroy(synthClip);

        foreach (AudioClip clip in generated)
            if (clip != null) Destroy(clip);
    }

    private void OnSynthSetPosition(int newPosition)
    {
        
    }

    private AudioSource CreateSource(string objectName)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(transform, false);

        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = spatialBlend;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.dopplerLevel = 0f;
        return src;
    }


   
    private void Update()
    {
        bool driven = car.isBeingDriven;
        bool flying = car.IsFlying;
        bool landing = car.IsLanding;
        bool flipping = car.IsFlipping;

        

        if (driven != prevDriven)
            PlayOneShot(driven ? clipPowerUp : clipPowerDown, driven ? 0.6f : 0.5f);

        if (flying && !prevFlying)
        {
            PlayOneShot(clipTakeoff, 1f);
        }
        else if (!flying && prevFlying)
        {
            PlayOneShot(clipTouchdown, 0.9f);
        }
        else if (flying)
        {
            if (landing && !prevLanding)
                PlayOneShot(clipLandingStart, 0.8f);
            else if (!landing && prevLanding)
                PlayOneShot(clipTakeoff, 0.7f, 1.2f);   
        }

        if (flipping && !prevFlipping)
            PlayOneShot(clipHyperTurn, 0.9f);

        
        if (driven && !flying && !flipping && !car.FlightUnlocked && Input.GetKeyDown(car.flightToggleKey))
            PlayOneShot(clipLocked, 0.5f);

        
        UpdateSynthParameters(driven, flying, flipping);

        prevDriven = driven;
        prevFlying = flying;
        prevLanding = landing;
        prevFlipping = flipping;
    }

    private void UpdateSynthParameters(bool driven, bool flying, bool flipping)
    {
        float spd = Mathf.Abs(car.CurrentSpeed);
        float refSpeed = flying ? car.maxFlightSpeed : car.maxSpeed;
        float speed01 = Mathf.Clamp01(spd / Mathf.Max(0.01f, refSpeed));

        
        float skid = 0f;

        if (driven && !flying && !flipping && car.IsGrounded)
        {
            if (car.ThrottleHeld && spd < car.maxSpeed * car.accelSmokeSpeedThreshold)
                skid = 0.45f;

            if (car.BrakeHeld && car.CurrentSpeed > car.brakeSmokeMinSpeed)
                skid = Mathf.Max(skid, Mathf.Clamp01(car.CurrentSpeed / Mathf.Max(0.01f, car.maxSpeed * 0.6f)));

            float corner = Mathf.Clamp01((Mathf.Abs(car.SteerInput) * speed01 - 0.5f) * 2.5f) * 0.6f;
            skid = Mathf.Max(skid, corner);
        }

        float vertical = flying
            ? Mathf.Clamp(car.FlightVerticalVelocity / Mathf.Max(0.01f, car.flightVerticalSpeed), -1f, 1f)
            : 0f;

        synth.engine = (driven || flying) ? 1f : 0f;
        synth.flight = flying ? 1f : 0f;
        synth.speed = speed01;
        synth.throttle = (driven && car.ThrottleHeld) ? 1f : 0f;
        synth.vertical = vertical;
        synth.skid = skid * tireSquealLevel;
        synth.reverse = (driven && !flying && car.CurrentSpeed < -0.5f) ? 1f : 0f;
        synth.charge = (driven && !flying) ? car.ReverseChargeProgress : 0f;
        synth.idle = idleHumLevel;
        synth.volume = masterVolume * engineVolume;
    }


    

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastImpactTime < impactSoundCooldown)
            return;

        float best = 0f;

        foreach (ContactPoint contact in collision.contacts)
        {
            
            if (Mathf.Abs(Vector3.Dot(contact.normal, Vector3.up)) > car.groundNormalThreshold)
                continue;

            float impact = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal));

            if (impact > best)
                best = impact;
        }

        if (best < impactSoundMinSpeed)
            return;

        lastImpactTime = Time.time;

        float strength = Mathf.InverseLerp(impactSoundMinSpeed, impactSoundMinSpeed + 14f, best);
        float pitch = Mathf.Lerp(1.15f, 0.8f, strength) * UnityEngine.Random.Range(0.95f, 1.05f);

        PlayOneShot(clipImpact, Mathf.Lerp(0.35f, 1f, strength), pitch);
    }




    private void PlayOneShot(AudioClip clip, float gain, float pitch = 1f)
    {
        if (clip == null || oneShots == null)
            return;

        AudioSource src = null;

        for (int i = 0; i < oneShots.Length; i++)
        {
            if (!oneShots[i].isPlaying)
            {
                src = oneShots[i];
                break;
            }
        }

        
        if (src == null)
        {
            src = oneShots[nextVictim];
            nextVictim = (nextVictim + 1) % oneShots.Length;
        }

        src.clip = clip;
        src.pitch = pitch;
        src.volume = Mathf.Clamp01(gain * effectsVolume * masterVolume);
        src.Play();
    }


   

    private static float Sin(double cycles)
    {
        return (float)Math.Sin(cycles * TwoPi);
    }

    private static float Smooth(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    
    private sealed class Osc
    {
        private double phase;
        private readonly double invSr;

        public Osc(int sampleRate) { invSr = 1.0 / sampleRate; }

        public double Tick(float freq)
        {
            phase += freq * invSr;
            phase -= Math.Floor(phase);
            return phase;
        }
    }

    
    private sealed class Noise
    {
        private uint state;

        public Noise(uint seed) { state = seed == 0 ? 1u : seed; }

        public float Next()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0xFFFFFF) / 8388608f - 1f;
        }
    }

    
    private sealed class OnePole
    {
        private float y;

        public float Process(float x, float a)
        {
            y += a * (x - y);
            return y;
        }
    }

    
    private sealed class ServoVoice
    {
        private readonly Osc whir, am, clunk;
        private readonly Noise noise = new Noise(77);
        private readonly OnePole lp = new OnePole();

        public ServoVoice(int sampleRate)
        {
            whir = new Osc(sampleRate);
            am = new Osc(sampleRate);
            clunk = new Osc(sampleRate);
        }

        public float Sample(float t, float start, float length, bool folding)
        {
            float st = t - start;
            if (st < 0f)
                return 0f;

            float s = 0f;

            if (st <= length)
            {
                float u = st / length;
                float f = folding ? Mathf.Lerp(320f, 820f, u) : Mathf.Lerp(820f, 320f, u);
                float env = Mathf.Sin(Mathf.PI * u);

                float saw = 2f * (float)whir.Tick(f) - 1f;
                float mod = 0.75f + 0.25f * Sin(am.Tick(34f));   

                s += 0.30f * env * mod * lp.Process(saw, 0.22f);
            }

            float ct = st - length;
            if (ct >= 0f && ct < 0.15f)
            {
                s += 0.45f * Mathf.Exp(-ct * 50f) * noise.Next();
                s += 0.35f * Mathf.Exp(-ct * 28f) * Sin(clunk.Tick(150f));
            }

            return s;
        }
    }


    

    
    private AudioClip MakeClip(string clipName, float seconds, Func<float, float> gen, float peak = 0.9f)
    {
        int n = Mathf.CeilToInt(seconds * sampleRate);
        float[] data = new float[n];
        float dt = 1f / sampleRate;
        float max = 0.0001f;

        for (int i = 0; i < n; i++)
        {
            float v = gen(i * dt);
            data[i] = v;

            float a = Mathf.Abs(v);
            if (a > max) max = a;
        }

        float gain = peak / max;
        int fadeIn = Mathf.Max(1, sampleRate / 500);    
        int fadeOut = Mathf.Max(1, sampleRate / 100);   

        for (int i = 0; i < n; i++)
        {
            float f = 1f;
            if (i < fadeIn) f = i / (float)fadeIn;
            if (i >= n - fadeOut) f = Mathf.Min(f, (n - i) / (float)fadeOut);
            data[i] *= gain * f;
        }

        AudioClip clip = AudioClip.Create(clipName, n, 1, sampleRate, false);
        clip.SetData(data, 0);
        generated.Add(clip);
        return clip;
    }

    private float ServoLength()
    {
        return Mathf.Clamp(car.wheelFoldDuration, 0.3f, 1.5f);
    }

    
    private AudioClip BuildChime(bool powerUp)
    {
        float[] notes = powerUp
            ? new[] { 523.25f, 783.99f, 1046.5f }
            : new[] { 1046.5f, 783.99f, 523.25f };

        Osc[] voices = { new Osc(sampleRate), new Osc(sampleRate), new Osc(sampleRate) };
        Osc sweep = new Osc(sampleRate);
        Noise noise = new Noise(11);
        OnePole hp = new OnePole();

        return MakeClip(powerUp ? "Car_PowerUp" : "Car_PowerDown", 1.0f, t =>
        {
            float s = 0f;

            for (int k = 0; k < 3; k++)
            {
                float tt = t - k * 0.11f;
                if (tt < 0f) continue;

                float env = Mathf.Exp(-tt * 6f) * Mathf.Min(1f, tt * 150f);
                double p = voices[k].Tick(notes[k]);
                s += 0.5f * env * (Sin(p) + 0.35f * Sin(p * 2.0));
            }

            float u = Mathf.Clamp01(t / 0.7f);
            float sf = powerUp ? Mathf.Lerp(90f, 520f, u * u) : Mathf.Lerp(520f, 70f, u);
            float sEnv = Mathf.Min(1f, t * 80f) * Mathf.Exp(-Mathf.Max(0f, t - 0.4f) * 6f);
            s += 0.30f * sEnv * Sin(sweep.Tick(sf));

            float n = noise.Next();
            s += 0.05f * (n - hp.Process(n, 0.3f)) * Mathf.Exp(-t * 5f);

            return s;
        });
    }

    
    private AudioClip BuildTakeoff()
    {
        const float len = 2.2f;

        Osc spool = new Osc(sampleRate), fifth = new Osc(sampleRate);
        Osc sub = new Osc(sampleRate), ting = new Osc(sampleRate);
        Noise noise = new Noise(23);
        OnePole whoosh = new OnePole();
        ServoVoice servo = new ServoVoice(sampleRate);
        float servoLen = ServoLength();

        return MakeClip("Car_Takeoff", len, t =>
        {
            float u = t / len;

            float f = 60f + 1100f * u * u;
            float env = Mathf.Min(1f, t * 8f) * (1f - Smooth((u - 0.7f) / 0.3f));
            double p = spool.Tick(f);
            float s = env * (0.5f * Sin(p) + 0.14f * (2f * (float)p - 1f) + 0.2f * Sin(fifth.Tick(f * 1.5f)));

            s += 0.9f * Mathf.Exp(-t * 4.5f) * Sin(sub.Tick(Mathf.Lerp(52f, 34f, Mathf.Min(1f, t * 2f))));

            float w = whoosh.Process(noise.Next(), 0.02f + 0.3f * u * u);
            s += 1.5f * w * Mathf.Sin(Mathf.PI * u);

            if (t > 0.9f)
            {
                float g = t - 0.9f;
                s += 0.22f * Mathf.Exp(-g * 5f) * Sin(ting.Tick(1318.5f));
            }

            s += servo.Sample(t, 0.12f, servoLen, true);
            return s;
        });
    }

    
    private AudioClip BuildLanding()
    {
        const float len = 1.7f;

        Osc spool = new Osc(sampleRate), fifth = new Osc(sampleRate);
        Noise noise = new Noise(31);
        OnePole whoosh = new OnePole();
        ServoVoice servo = new ServoVoice(sampleRate);
        float servoLen = ServoLength();

        return MakeClip("Car_LandingStart", len, t =>
        {
            float u = t / len;

            float f = 80f + 950f * Mathf.Exp(-u * 3.4f);
            float env = Mathf.Min(1f, t * 20f) * (1f - Smooth((u - 0.55f) / 0.45f));
            double p = spool.Tick(f);
            float s = env * (0.5f * Sin(p) + 0.14f * (2f * (float)p - 1f) + 0.2f * Sin(fifth.Tick(f * 1.5f)));

            float w = whoosh.Process(noise.Next(), 0.02f + 0.25f * (1f - u) * (1f - u));
            s += 1.3f * w * (1f - u);

            s += servo.Sample(t, 0.1f, servoLen, false);
            return s;
        });
    }

    
    private AudioClip BuildTouchdown()
    {
        Osc thump = new Osc(sampleRate), boing = new Osc(sampleRate);
        Noise noise = new Noise(41);
        OnePole lp = new OnePole();

        return MakeClip("Car_Touchdown", 0.8f, t =>
        {
            float s = Mathf.Exp(-t * 9f) * Sin(thump.Tick(Mathf.Lerp(95f, 38f, Mathf.Min(1f, t * 7f))));

            float n = noise.Next();
            float hiss = n - lp.Process(n, 0.25f);
            s += 0.35f * hiss * Mathf.Exp(-t * 6f) * Mathf.Min(1f, t * 60f);

            s += 0.12f * Mathf.Exp(-t * 12f) * Sin(boing.Tick(210f));
            return s;
        });
    }

    
    private AudioClip BuildHyperTurn()
    {
        Osc zap = new Osc(sampleRate), ringA = new Osc(sampleRate), ringB = new Osc(sampleRate), sub = new Osc(sampleRate);
        Noise noise = new Noise(67);

        return MakeClip("Car_HyperTurn", 1.0f, t =>
        {
            float f = 110f + 2600f * Mathf.Exp(-t * 9f);
            double p = zap.Tick(f);
            float s = 0.55f * Mathf.Min(1f, t * 300f) * Mathf.Exp(-t * 3.2f) * (0.6f * (2f * (float)p - 1f) + 0.4f * Sin(p));

            s += 0.45f * Mathf.Exp(-t * 11f) * noise.Next();
            s += 0.22f * Mathf.Exp(-t * 4f) * (Sin(ringA.Tick(880f)) + 0.7f * Sin(ringB.Tick(1327f)));
            s += 0.6f * Mathf.Exp(-t * 6f) * Sin(sub.Tick(70f));
            return s;
        });
    }

    
    private AudioClip BuildLocked()
    {
        Osc tone = new Osc(sampleRate);
        OnePole lp = new OnePole();

        return MakeClip("Car_FlightLocked", 0.45f, t =>
        {
            float g = 0f;

            if (t < 0.14f)
                g = Mathf.Min(1f, Mathf.Min(t, 0.14f - t) * 120f);
            else if (t >= 0.2f && t < 0.38f)
                g = Mathf.Min(1f, Mathf.Min(t - 0.2f, 0.38f - t) * 120f);

            double p = tone.Tick(t < 0.19f ? 330f : 247f);
            float square = p < 0.5 ? 1f : -1f;

            return g * lp.Process(square, 0.2f);
        });
    }

    
    private AudioClip BuildImpact()
    {
        Osc thump = new Osc(sampleRate), ring = new Osc(sampleRate);
        Noise noise = new Noise(53), spark = new Noise(59);
        OnePole crunchLp = new OnePole(), sparkLp = new OnePole();

        return MakeClip("Car_Impact", 0.8f, t =>
        {
            float s = 0.9f * Mathf.Exp(-t * 10f) * Sin(thump.Tick(Mathf.Lerp(120f, 45f, Mathf.Min(1f, t * 8f))));

            s += 1.4f * Mathf.Exp(-t * 16f) * crunchLp.Process(noise.Next(), 0.3f);

            float sp = spark.Next();
            float crackle = Mathf.Abs(sp) > 0.96f ? sp : 0f;
            s += 0.5f * Mathf.Exp(-t * 5f) * (crackle - sparkLp.Process(crackle, 0.4f));

            s += 0.25f * Mathf.Exp(-t * 14f) * Sin(ring.Tick(540f));
            return s;
        });
    }


    
    private sealed class Synth
    {
        
        public volatile float engine;      
        public volatile float flight;      
        public volatile float speed;       
        public volatile float throttle;   
        public volatile float vertical;    
        public volatile float skid;        
        public volatile float reverse;     
        public volatile float charge;      
        public volatile float idle;        
        public volatile float volume;      

        private readonly double invSr;
        private readonly float cEngine, cFlight, cSpeed, cThrottle, cVert, cSkid, cReverse, cCharge, cChargeFreq;

        
        private float sEngine, sFlight, sSpeed, sThrottle, sVert, sSkid, sReverse, sCharge;
        private float chargeFreq = 220f;

        
        private double motorPh, motorPh2, whinePh, lfoVib;
        private double hoverPh1, hoverPh2, lfoWub, shimPh, lfoShim;
        private double lfoSkid, squealPh;
        private double beepPh;
        private double chargePh, chargePh2, lfoCharge;

        
        private float turbLp, windLp1, windLp2;
        private float svfLow, svfBand;
        private float beepT;
        private bool beepHigh;

        private readonly Noise noise = new Noise(12345);

        public Synth(int sampleRate)
        {
            invSr = 1.0 / sampleRate;

            cEngine = Coef(0.45f, sampleRate);
            cFlight = Coef(0.60f, sampleRate);
            cSpeed = Coef(0.12f, sampleRate);
            cThrottle = Coef(0.10f, sampleRate);
            cVert = Coef(0.25f, sampleRate);
            cSkid = Coef(0.05f, sampleRate);
            cReverse = Coef(0.02f, sampleRate);
            cCharge = Coef(0.05f, sampleRate);
            cChargeFreq = Coef(0.01f, sampleRate);
        }

        private static float Coef(float tau, int sr)
        {
            return 1f - (float)Math.Exp(-1.0 / (tau * sr));
        }

        private void Adv(ref double phase, double hz)
        {
            phase += hz * invSr;
            if (phase >= 1.0) phase -= 1.0;
        }

        public void Fill(float[] data)
        {
            float tEngine = engine, tFlight = flight, tSpeed = speed, tThrottle = throttle;
            float tVert = vertical, tSkid = skid, tReverse = reverse, tCharge = charge;
            float vol = volume, idleLvl = idle;

            float chargeTarget = (float)(220.0 * Math.Pow(8.0, tCharge));

            for (int i = 0; i < data.Length; i++)
            {
                sEngine += (tEngine - sEngine) * cEngine;
                sFlight += (tFlight - sFlight) * cFlight;
                sSpeed += (tSpeed - sSpeed) * cSpeed;
                sThrottle += (tThrottle - sThrottle) * cThrottle;
                sVert += (tVert - sVert) * cVert;
                sSkid += (tSkid - sSkid) * cSkid;
                sReverse += (tReverse - sReverse) * cReverse;
                sCharge += (tCharge - sCharge) * cCharge;
                chargeFreq += (chargeTarget - chargeFreq) * cChargeFreq;

                float ground = sEngine * (1f - sFlight);
                float air = sEngine * sFlight;

                float s = 0f;

                if (ground > 0.0005f) s += Motor(ground, idleLvl);
                if (air > 0.0005f) s += Hover(air);

                s += Wind();
                s += Skid();
                s += Beep();
                s += Charge();

                data[i] = (float)Math.Tanh(s * 1.5f) * vol;
            }
        }

        
        private float Motor(float level, float idleLvl)
        {
            float f0 = 48f + sSpeed * 150f + sThrottle * 12f;

            Adv(ref motorPh, f0);
            Adv(ref motorPh2, f0 * 1.007);

            float tone =
                Sin(motorPh) +
                0.45f * Sin(motorPh * 2.0) +
                0.2f * Sin(motorPh * 3.0) +
                0.5f * Sin(motorPh2);

            Adv(ref lfoVib, 6.5);
            Adv(ref whinePh, 520.0 + sSpeed * 2600.0 + sThrottle * 140.0);
            float whine = (float)Math.Sin(whinePh * TwoPi + 0.6 * Math.Sin(lfoVib * TwoPi));

            float toneAmp = idleLvl * 0.22f + 0.11f * sSpeed + 0.07f * sThrottle;
            float whineAmp = idleLvl * 0.020f + 0.06f * sSpeed + 0.03f * sThrottle;

            return level * (tone * toneAmp + whine * whineAmp);
        }

        
        private float Hover(float level)
        {
            float hf = 46f * (1f + 0.15f * sVert);

            Adv(ref hoverPh1, hf);
            Adv(ref hoverPh2, hf * 1.013);
            Adv(ref lfoWub, 2.4 + 2.0 * Math.Abs(sVert));

            float wub = 1f + 0.18f * Sin(lfoWub);
            float drone = (Sin(hoverPh1) + 0.8f * Sin(hoverPh2) + 0.3f * Sin(hoverPh1 * 2.0)) * 0.34f * wub;

            Adv(ref lfoShim, 5.5);
            Adv(ref shimPh, 1250.0 + 260.0 * sVert + 35.0 * Sin(lfoShim));
            float shimmer = Sin(shimPh) * (0.03f + 0.03f * Math.Abs(sVert) + 0.02f * sSpeed);

            turbLp += (0.03f + 0.25f * sSpeed) * (noise.Next() - turbLp);
            float turbine = turbLp * (1.2f + 2.4f * sSpeed);

            return level * (drone + shimmer + turbine * 0.5f);
        }

        
        private float Wind()
        {
            float w = sSpeed;
            float n = noise.Next();

            windLp1 += (0.04f + 0.30f * w) * (n - windLp1);
            windLp2 += 0.01f * (n - windLp2);

            return (windLp1 - windLp2) * 1.4f * w * w;
        }

        
        private float Skid()
        {
            if (sSkid < 0.003f)
                return 0f;

            Adv(ref lfoSkid, 9.0);
            double fc = 1900.0 + 450.0 * Sin(lfoSkid);
            float f = (float)(2.0 * Math.Sin(Math.PI * fc * invSr));

            float n = noise.Next();
            svfLow += f * svfBand;
            float high = n - svfLow - 0.14f * svfBand;
            svfBand += f * high;

            Adv(ref squealPh, 1350.0 + 120.0 * Sin(lfoSkid * 0.5));
            float squeal = Sin(squealPh) * 0.04f;

            return sSkid * (svfBand * 0.55f + squeal);
        }

        
        private float Beep()
        {
            if (sReverse < 0.003f)
            {
                beepT = 0f;
                return 0f;
            }

            const float period = 0.45f;
            const float on = 0.2f;

            beepT += (float)invSr;
            if (beepT >= period)
            {
                beepT -= period;
                beepHigh = !beepHigh;
            }

            if (beepT >= on)
                return 0f;

            float env = (float)Math.Sin(Math.PI * beepT / on);
            Adv(ref beepPh, beepHigh ? 1320.0 : 880.0);

            return sReverse * env * 0.09f * (Sin(beepPh) + 0.3f * Sin(beepPh * 2.0));
        }

        
        private float Charge()
        {
            if (sCharge < 0.003f)
                return 0f;

            Adv(ref chargePh, chargeFreq);
            Adv(ref chargePh2, chargeFreq * 2.01);
            Adv(ref lfoCharge, 5.0 + 16.0 * sCharge);

            float trem = 0.65f + 0.35f * Sin(lfoCharge);
            float amp = 0.03f + 0.20f * sCharge;

            return trem * amp * (Sin(chargePh) + 0.4f * Sin(chargePh2));
        }
    }
}