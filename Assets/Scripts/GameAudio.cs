
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

    private void Awake()
    {
        Instance = this;
    }

    private void Play(AudioClip clip)
    {
        if (source != null && clip != null)
        {
            source.PlayOneShot(clip);
        }
    }

    public void PlayWind() => Play(wind);
    public void PlayLaunch() => Play(launch);
    public void PlayPickup() => Play(pickup);
    public void PlayHit() => Play(hit);
    public void PlayGameOver() => Play(gameOver);
}
