using System;
using System.Buffers.Binary;
using Godot;

namespace ComputerLive;

internal sealed class WorldSound
{
    private const int Rate = 16000;
    private readonly AudioStreamPlayer _step = new() { VolumeDb = -18 };
    private readonly AudioStreamPlayer _tram = new() { VolumeDb = -21 };
    private readonly AudioStreamWav _concrete;
    private readonly AudioStreamWav _pavement;
    private float _distance;
    private bool _alternate;

    public WorldSound(Node parent)
    {
        parent.AddChild(_step);
        parent.AddChild(_tram);
        _concrete = StepSample(135, 0.12);
        _pavement = StepSample(88, 0.26);
        _tram.Stream = Sample(1.0, (time, noise) =>
        {
            double pulse = 0.51 + Math.Pow(Math.Sin(time * Math.PI * 4), 6) * 0.45;
            return (Math.Sin(time * Math.Tau * 42) * 0.22 + Math.Sin(time * Math.Tau * 80) * 0.13 + noise * 0.31) * pulse;
        }, loop: true);
    }

    public void ChangeLocation(Place place)
    {
        _distance = 0;
        _step.Stop();
        if (place == Place.Tram)
            _tram.Play();
        else
            _tram.Stop();
    }

    public void Footsteps(float distance, bool outdoors)
    {
        _distance += distance;
        if (_distance < 1.40f)
            return;

        _distance -= 1.40f;
        _alternate = !_alternate;
        _step.Stream = outdoors ? _pavement : _concrete;
        _step.PitchScale = _alternate ? 0.95f : 1.07f;
        _step.Play();
    }

    private static AudioStreamWav StepSample(int frequency, double grit) => Sample(0.19, (time, noise) =>
        (Math.Sin(time * Math.Tau * frequency) * 0.50 + noise * grit) * Math.Exp(-time * 33) * Math.Min(time * 450, 1));

    private static AudioStreamWav Sample(double duration, Func<double, double, double> waveform, bool loop = false)
    {
        int frames = (int)(duration * Rate);
        var data = new byte[frames * sizeof(short)];
        var random = new Random(17);
        double smoothedNoise = 0;
        for (int frame = 0; frame < frames; frame++)
        {
            smoothedNoise = smoothedNoise * 0.72 + (random.NextDouble() * 2 - 1) * 0.28;
            var sample = (short)(waveform((double)frame / Rate, smoothedNoise) * short.MaxValue);
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(frame * sizeof(short)), sample);
        }

        return new AudioStreamWav
        {
            Data = data, MixRate = Rate, Format = AudioStreamWav.FormatEnum.Format16Bits,
            LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled, LoopEnd = frames,
        };
    }
}
