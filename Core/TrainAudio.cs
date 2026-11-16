using System;
using Microsoft.Xna.Framework.Audio;

namespace videojuego.Core;

public class TrainAudio : IDisposable
{
    private const int SampleRate = 44100;

    private SoundEffect _motorBaseEffect;
    private SoundEffectInstance _motorInstance;

    private SoundEffect _doorChimeEffect;
    private SoundEffect _notchClickEffect;
    private SoundEffect _atpAlarmEffect;

    private float _alarmCooldown = 0f;

    public TrainAudio()
    {
        // 1. Generar onda continua para el motor eléctrico (1 segundo en bucle)
        _motorBaseEffect = CreateSineLoop(110f, 1.0f);
        _motorInstance = _motorBaseEffect.CreateInstance();
        _motorInstance.IsLooped = true;
        _motorInstance.Volume = 0f;
        _motorInstance.Play();

        // 2. Generar aviso bitonal de cierre de puertas de metro (dos notas descendentes)
        _doorChimeEffect = CreateTwoToneChime(880f, 659.25f, 0.22f);

        // 3. Generar clic mecánico de palanca de muescas
        _notchClickEffect = CreateMechanicalClick(0.035f);

        // 4. Generar pitido agudo de advertencia ATP
        _atpAlarmEffect = CreateSquareBeep(1400f, 0.12f);
    }

    public void Update(float dt, float velocityKmh, int notch, bool overspeed, bool emergency)
    {
        // Modular el sonido del motor eléctrico según velocidad y potencia aplicada
        if (velocityKmh > 0.2f || notch > 0)
        {
            float speedRatio = Math.Clamp(velocityKmh / 80f, 0f, 1f);
            float loadBoost = notch > 0 ? 0.12f : (notch < 0 ? 0.08f : 0.02f);

            _motorInstance.Volume = Math.Clamp((speedRatio * 0.25f) + loadBoost, 0f, 0.45f);
            // El tono sube desde grave (-0.6) hasta agudo (+0.85) como un variador VVVF
            _motorInstance.Pitch = Math.Clamp(-0.6f + (speedRatio * 1.45f), -1.0f, 1.0f);
        }
        else
        {
            _motorInstance.Volume = 0f;
        }

        // Disparar pitido periódico si hay exceso de velocidad o emergencia ATP
        if (_alarmCooldown > 0f)
            _alarmCooldown -= dt;

        if ((overspeed || emergency) && _alarmCooldown <= 0f)
        {
            _atpAlarmEffect.Play(0.35f, 0f, 0f);
            _alarmCooldown = emergency ? 0.35f : 0.65f;
        }
    }

    public void PlayDoorChime()
    {
        _doorChimeEffect.Play(0.45f, 0f, 0f);
    }

    public void PlayNotchClick()
    {
        _notchClickEffect.Play(0.30f, 0f, 0f);
    }

    // --- Generadores matemáticos de audio PCM 16-bit Mono ---

    private SoundEffect CreateSineLoop(float frequency, float durationSeconds)
    {
        int sampleCount = (int)(SampleRate * durationSeconds);
        byte[] buffer = new byte[sampleCount * 2];

        for (int i = 0; i < sampleCount; i++)
        {
            double t = (double)i / SampleRate;
            // Combinamos fundamental + armónico suave para darle cuerpo de motor eléctrico
            double wave = Math.Sin(2.0 * Math.PI * frequency * t) * 0.7
                        + Math.Sin(2.0 * Math.PI * (frequency * 2.0) * t) * 0.3;

            short sample = (short)(wave * short.MaxValue * 0.5);
            WriteSample(buffer, i, sample);
        }

        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private SoundEffect CreateTwoToneChime(float freq1, float freq2, float toneDuration)
    {
        int singleSamples = (int)(SampleRate * toneDuration);
        int totalSamples = singleSamples * 2;
        byte[] buffer = new byte[totalSamples * 2];

        for (int i = 0; i < totalSamples; i++)
        {
            bool isSecondTone = i >= singleSamples;
            int localIndex = isSecondTone ? (i - singleSamples) : i;
            float freq = isSecondTone ? freq2 : freq1;

            double t = (double)localIndex / SampleRate;
            // Envolvente de decaimiento suave tipo timbre electrónico
            double envelope = Math.Exp(-6.0 * ((double)localIndex / singleSamples));
            double wave = Math.Sin(2.0 * Math.PI * freq * t) * envelope;

            short sample = (short)(wave * short.MaxValue * 0.6);
            WriteSample(buffer, i, sample);
        }

        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private SoundEffect CreateSquareBeep(float frequency, float durationSeconds)
    {
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
        buffer[index * 2] = (byte)(sample & 0xFF);
        buffer[index * 2 + 1] = (byte)((sample >> 8) & 0xFF);
    }

    public void Dispose()
    {
        _motorInstance?.Stop();
        _motorInstance?.Dispose();
        _motorBaseEffect?.Dispose();
        _doorChimeEffect?.Dispose();
        _notchClickEffect?.Dispose();
        _atpAlarmEffect?.Dispose();
    }
}