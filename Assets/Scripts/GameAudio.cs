using UnityEngine;

public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }
    public AudioSource source;
    public AudioClip wind;
    public AudioClip launch;
    public AudioClip pickup;
    public AudioClip hit;
    public AudioClip gameOver;
    [SerializeField, Range(0f, 1f)] private float windVolume = 0.12f;
    private AudioSource windSource;
    private float gustRemaining;
    private const float GustDuration = 0.5f;

    private void Awake()
    {
        Instance = this;
        windSource = gameObject.AddComponent<AudioSource>();
        windSource.playOnAwake = false;
        windSource.spatialBlend = 0f;
        windSource.clip = wind;
        if (source != null) windSource.outputAudioMixerGroup = source.outputAudioMixerGroup;
    }

    private void Update()
    {
        if (!windSource.isPlaying) return;
        gustRemaining = Mathf.Max(0f, gustRemaining - Time.deltaTime);
        float attack = Mathf.Clamp01((GustDuration - gustRemaining) / 0.04f);
        float release = Mathf.Clamp01(gustRemaining / 0.18f);
        windSource.volume = windVolume * Mathf.Min(attack, release);
        if (gustRemaining <= 0f) windSource.Stop();
    }

    private void Play(AudioClip clip)
    {
        if (source != null && clip != null) source.PlayOneShot(clip);
    }

    public void PlayWind()
    {
        if (wind == null || windSource == null) return;
        // One voice, no stacked six-second clips or hard restarts on rapid swipes.
        if (!windSource.isPlaying)
        {
            windSource.volume = 0f;
            windSource.Play();
            gustRemaining = GustDuration;
        }
        else gustRemaining = Mathf.Max(gustRemaining, GustDuration - 0.04f);
    }

    public void StopWind()
    {
        if (windSource != null) windSource.Stop();
        gustRemaining = 0f;
    }

    public void PlayLaunch() => Play(launch);
    public void PlayPickup() => Play(pickup);
    public void PlayHit() => Play(hit);
    public void PlayGameOver() => Play(gameOver);

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
