using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using NAudio.Wave;

namespace videojuego.Core;

/// <summary>
/// Gestiona todo el audio del simulador: motor eléctrico continuo, megafonía de estaciones (MP3 vía NAudio), y efectos puntuales (timbre de puertas, clic de muesca, alarma ATP). Implementa IDisposable para liberar recursos nativos de audio.
/// </summary>
public class TrainAudio : IDisposable
{
    private const int SampleRate = 44100;

    private SoundEffect _motorBaseEffect;
    private SoundEffectInstance _motorInstance;

    private SoundEffect _doorChimeEffect;
    private SoundEffect _notchClickEffect;
    private SoundEffect _atpAlarmEffect;

    private WaveOutEvent _announcementOut;
    private AudioFileReader _announcementReader;
    private float _alarmCooldown = 0f;

    public bool IsAnnouncementPlaying =>
        _announcementOut != null && _announcementOut.PlaybackState == PlaybackState.Playing;

    public TrainAudio()
    {
        // [BLOQUE: Inicialización del motor eléctrico en bucle]
        // 1. Generar onda continua para el motor eléctrico (1 segundo en bucle)
        _motorBaseEffect = CreateSineLoop(110f, 1.0f);
        _motorInstance = _motorBaseEffect.CreateInstance();
        _motorInstance.IsLooped = true;
        _motorInstance.Volume = 0f;
        _motorInstance.Play();

        // [BLOQUE: Generación del aviso bitonal de cierre de puertas]
        // 2. Generar aviso bitonal de cierre de puertas de metro
        _doorChimeEffect = CreateTwoToneChime(880f, 659.25f, 0.22f);

        // [BLOQUE: Generación del clic mecánico de palanca de muescas]
        // 3. Generar clic mecánico de palanca de muescas
        _notchClickEffect = CreateMechanicalClick(0.035f);

        // [BLOQUE: Generación de la alarma sonora ATP]
        // 4. Generar pitido agudo de advertencia ATP
        _atpAlarmEffect = CreateSquareBeep(1400f, 0.12f);
    }

    public void Update(float dt, float velocityKmh, int notch, bool overspeed, bool emergency)
    {
        // [BLOQUE: Modulación del sonido del motor eléctrico según velocidad, carga y ducking]
        if (velocityKmh > 0.2f || notch > 0)
        {
            float speedRatio = Math.Clamp(velocityKmh / 80f, 0f, 1f);
            float loadBoost = notch > 0 ? 0.12f : (notch < 0 ? 0.08f : 0.02f);
            float ducking = IsAnnouncementPlaying ? 0.45f : 1.0f;

            _motorInstance.Volume = Math.Clamp(((speedRatio * 0.25f) + loadBoost) * ducking, 0f, 0.45f);
            _motorInstance.Pitch = Math.Clamp(-0.6f + (speedRatio * 1.45f), -1.0f, 1.0f);
        }
        else
        {
            _motorInstance.Volume = 0f;
        }

        // [BLOQUE: Gestión y disparo de la alarma sonora periódica ATP/sobrevelocidad]
        if (_alarmCooldown > 0f)
            _alarmCooldown -= dt;

        if ((overspeed || emergency) && _alarmCooldown <= 0f)
        {
            _atpAlarmEffect.Play(0.35f, 0f, 0f);
            _alarmCooldown = emergency ? 0.35f : 0.65f;
        }
    }

    public void PlayStationAnnouncement(string fileName)
    {
        // [BLOQUE: Validación del archivo de audio y resolución de ruta]
        if (string.IsNullOrEmpty(fileName)) return;

        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", fileName);
        if (!File.Exists(fullPath)) return;

        // [BLOQUE: Reproducción de megafonía mediante NAudio]
        try
        {
            StopAnnouncement();

            _announcementReader = new AudioFileReader(fullPath);
            _announcementReader.Volume = 0.90f;

            _announcementOut = new WaveOutEvent();
            _announcementOut.Init(_announcementReader);
            _announcementOut.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reproduciendo {fileName}: {ex.Message}");
        }
    }

    public void StopAnnouncement()
    {
        // [BLOQUE: Detención y liberación del reproductor de megafonía]
        try
        {
            _announcementOut?.Stop();
            _announcementOut?.Dispose();
            _announcementOut = null;

            _announcementReader?.Dispose();
            _announcementReader = null;
        }
        catch { }
    }

    public void PlayDoorChime()
    {
        // [BLOQUE: Reproducción del aviso sonoro de puertas]
        _doorChimeEffect.Play(0.45f, 0f, 0f);
    }

    public void PlayNotchClick()
    {
        // [BLOQUE: Reproducción del clic mecánico de muesca]
        _notchClickEffect.Play(0.30f, 0f, 0f);
    }

    // --- Generadores matemáticos de audio PCM 16-bit Mono ---

    private SoundEffect CreateSineLoop(float frequency, float durationSeconds)
    {
        // [BLOQUE: Síntesis de onda senoidal con armónicos para bucle de motor]
        int sampleCount = (int)(SampleRate * durationSeconds);
        byte[] buffer = new byte[sampleCount * 2];

        for (int i = 0; i < sampleCount; i++)
        {
            double t = (double)i / SampleRate;
            double wave = Math.Sin(2.0 * Math.PI * frequency * t) * 0.7
                        + Math.Sin(2.0 * Math.PI * (frequency * 2.0) * t) * 0.3;

            short sample = (short)(wave * short.MaxValue * 0.5);
            WriteSample(buffer, i, sample);
        }

        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private SoundEffect CreateTwoToneChime(float freq1, float freq2, float toneDuration)
    {
        // [BLOQUE: Síntesis aditiva bitonal con envolvente exponencial decreciente]
        int singleSamples = (int)(SampleRate * toneDuration);
        int totalSamples = singleSamples * 2;
        byte[] buffer = new byte[totalSamples * 2];

        for (int i = 0; i < totalSamples; i++)
        {
            bool isSecondTone = i >= singleSamples;
            int localIndex = isSecondTone ? (i - singleSamples) : i;
            float freq = isSecondTone ? freq2 : freq1;

            double t = (double)localIndex / SampleRate;
            double envelope = Math.Exp(-6.0 * ((double)localIndex / singleSamples));
            double wave = Math.Sin(2.0 * Math.PI * freq * t) * envelope;

            short sample = (short)(wave * short.MaxValue * 0.6);
            WriteSample(buffer, i, sample);
        }

        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private SoundEffect CreateSquareBeep(float frequency, float durationSeconds)
    {
        // [BLOQUE: Síntesis de onda cuadrada para alarma sonora]
        int sampleCount = (int)(SampleRate * durationSeconds);
        byte[] buffer = new byte[sampleCount * 2];

        for (int i = 0; i < sampleCount; i++)
        {
            double t = (double)i / SampleRate;
            double wave = Math.Sin(2.0 * Math.PI * frequency * t) >= 0 ? 0.5 : -0.5;
            short sample = (short)(wave * short.MaxValue * 0.4);
            WriteSample(buffer, i, sample);
        }

        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private SoundEffect CreateMechanicalClick(float durationSeconds)
    {
        // [BLOQUE: Síntesis de ruido blanco con caída exponencial rápida]
        int sampleCount = (int)(SampleRate * durationSeconds);
        byte[] buffer = new byte[sampleCount * 2];
        Random rng = new Random(42);

        for (int i = 0; i < sampleCount; i++)
        {
            double decay = Math.Exp(-45.0 * ((double)i / sampleCount));
            double noise = (rng.NextDouble() * 2.0 - 1.0) * decay;
            short sample = (short)(noise * short.MaxValue * 0.6);
            WriteSample(buffer, i, sample);
        }

        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private static void WriteSample(byte[] buffer, int index, short sample)
    {
        // [BLOQUE: Escritura de muestra PCM de 16 bits en formato little-endian]
        buffer[index * 2] = (byte)(sample & 0xFF);
        buffer[index * 2 + 1] = (byte)((sample >> 8) & 0xFF);
    }

    public void Dispose()
    {
        // [BLOQUE: Liberación de instancias y efectos de sonido nativos]
        StopAnnouncement();
        _motorInstance?.Stop();
        _motorInstance?.Dispose();
        _motorBaseEffect?.Dispose();
        _doorChimeEffect?.Dispose();
        _notchClickEffect?.Dispose();
        _atpAlarmEffect?.Dispose();
    }

    public void PauseAll()
    {
        // [BLOQUE: Pausa de todas las fuentes de audio activas]
        if (_motorInstance != null && _motorInstance.State == SoundState.Playing)
            _motorInstance.Pause();

        if (_announcementOut != null && _announcementOut.PlaybackState == PlaybackState.Playing)
            _announcementOut.Pause();
    }

    public void ResumeAll()
    {
        // [BLOQUE: Reanudación de las fuentes de audio pausadas]
        if (_motorInstance != null && _motorInstance.State == SoundState.Paused)
            _motorInstance.Play();

        if (_announcementOut != null && _announcementOut.PlaybackState == PlaybackState.Paused)
            _announcementOut.Play();
    }
}
