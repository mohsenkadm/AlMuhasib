using System.Buffers.Binary;

const int SampleRate = 44100;
var outputDir = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "AlMuhasib.UI", "Assets", "Sounds"));

Directory.CreateDirectory(outputDir);

var sounds = new Dictionary<string, Func<float[]>>
{
    ["success.wav"] = () => Chime([523.25, 659.25, 783.99], 0.085, 0.30),
    ["save.wav"] = () => Combine(Tone(880, 0.04, 0.18, 0.06), Chime([659.25, 783.99], 0.07, 0.26)),
    ["delete.wav"] = () => Combine(Whoosh(0.12), Tone(220, 0.08, 0.22, 0.05)),
    ["error.wav"] = () => Combine(Tone(180, 0.12, 0.30, 0.08, 0.1), NoiseBurst(0.05, 0.12, 140)),
    ["warning.wav"] = () => Combine(Tone(440, 0.07, 0.28), Tone(554, 0.09, 0.28)),
    ["verify.wav"] = () => Chime([880, 1174.66], 0.06, 0.28),
    ["confirm.wav"] = () => Tone(698.46, 0.07, 0.26, 0.12, 0.15),
    ["cancel.wav"] = () => Tone(349.23, 0.08, 0.22, 0.10, 0.1),
    ["info.wav"] = () => Chime([587.33, 739.99], 0.07, 0.24),
    ["click.wav"] = () => Tone(1200, 0.025, 0.15, 0.02),
    ["scan.wav"] = ScanBeep,
    ["login.wav"] = () => Chime([392, 523.25, 659.25, 783.99], 0.065, 0.27),
    ["notification.wav"] = () => Chime([659.25, 880], 0.075, 0.25),
};

foreach (var (name, generator) in sounds)
{
    var path = Path.Combine(outputDir, name);
    WriteWav(path, generator());
    Console.WriteLine($"Generated {name}");
}

Console.WriteLine($"Done -> {outputDir}");

static void WriteWav(string path, float[] samples)
{
    var dataSize = samples.Length * 2;
    using var fs = File.Create(path);
    using var bw = new BinaryWriter(fs);

    bw.Write("RIFF"u8);
    bw.Write(36 + dataSize);
    bw.Write("WAVE"u8);
    bw.Write("fmt "u8);
    bw.Write(16);
    bw.Write((short)1);
    bw.Write((short)1);
    bw.Write(SampleRate);
    bw.Write(SampleRate * 2);
    bw.Write((short)2);
    bw.Write((short)16);
    bw.Write("data"u8);
    bw.Write(dataSize);

    Span<byte> buffer = stackalloc byte[2];
    foreach (var sample in samples)
    {
        var clamped = Math.Clamp(sample, -1f, 1f);
        BinaryPrimitives.WriteInt16LittleEndian(buffer, (short)(clamped * 32767));
        bw.Write(buffer);
    }
}

static float Envelope(int index, int total, double attack = 0.02, double release = 0.15)
{
    var attackSamples = (int)(attack * SampleRate);
    var releaseSamples = (int)(release * SampleRate);
    var attackEnv = index < attackSamples ? index / (double)attackSamples : 1.0;
    var releaseStart = total - releaseSamples;
    var releaseEnv = index > releaseStart ? (total - index) / (double)releaseSamples : 1.0;
    return (float)(attackEnv * releaseEnv);
}

static float[] Tone(double frequency, double duration, double volume, double release = 0.12, double harmonic = 0.25)
{
    var total = (int)(duration * SampleRate);
    var samples = new float[total];
    for (var i = 0; i < total; i++)
    {
        var t = i / (double)SampleRate;
        var env = Envelope(i, total, 0.008, release);
        var fund = Math.Sin(2 * Math.PI * frequency * t);
        var harm = Math.Sin(2 * Math.PI * frequency * 2 * t) * harmonic;
        samples[i] = (float)((fund + harm) * env * volume);
    }
    return samples;
}

static float[] Chime(double[] frequencies, double noteDuration, double volume)
{
    var list = new List<float>();
    foreach (var freq in frequencies)
    {
        list.AddRange(Tone(freq, noteDuration, volume, 0.18));
        list.AddRange(new float[(int)(0.012 * SampleRate)]);
    }
    return list.ToArray();
}

static float[] NoiseBurst(double duration, double volume, double frequency)
{
    var total = (int)(duration * SampleRate);
    var samples = new float[total];
    var rng = new Random(42);
    for (var i = 0; i < total; i++)
    {
        var t = i / (double)SampleRate;
        var env = Envelope(i, total, 0.001, 0.04);
        var noise = (rng.NextDouble() * 2 - 1) * 0.4;
        var tone = Math.Sin(2 * Math.PI * frequency * t) * 0.6;
        samples[i] = (float)((noise + tone) * env * volume);
    }
    return samples;
}

static float[] Whoosh(double duration, double startFreq = 900, double endFreq = 200)
{
    var total = (int)(duration * SampleRate);
    var samples = new float[total];
    for (var i = 0; i < total; i++)
    {
        var progress = i / (double)total;
        var freq = startFreq + (endFreq - startFreq) * progress;
        var t = i / (double)SampleRate;
        var env = Envelope(i, total, 0.005, 0.08);
        samples[i] = (float)(Math.Sin(2 * Math.PI * freq * t) * env * 0.28);
    }
    return samples;
}

static float[] ScanBeep()
{
    const double frequency = 1240;
    const double duration = 0.05;
    var total = (int)(duration * SampleRate);
    var samples = new float[total];
    for (var i = 0; i < total; i++)
    {
        var t = i / (double)SampleRate;
        var env = Envelope(i, total, 0.002, 0.03);
        var wave = Math.Sin(2 * Math.PI * frequency * t) > 0 ? 1.0 : -0.3;
        samples[i] = (float)(wave * env * 0.22);
    }
    return samples;
}

static float[] Combine(params float[][] parts)
{
    var total = parts.Sum(p => p.Length);
    var result = new float[total];
    var offset = 0;
    foreach (var part in parts)
    {
        Array.Copy(part, 0, result, offset, part.Length);
        offset += part.Length;
    }
    return result;
}
