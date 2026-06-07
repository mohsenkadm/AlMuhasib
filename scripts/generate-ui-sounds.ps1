# Generates short, pleasant UI sound effects as 16-bit mono WAV files.
param(
    [string]$OutputDir = (Join-Path $PSScriptRoot "..\src\AlMuhasib.UI\Assets\Sounds")
)

$SampleRate = 44100

function Write-WavFile {
    param([string]$Path, [float[]]$Samples)

    $count = $Samples.Length
    $dataSize = $count * 2
    $fs = [System.IO.File]::Create($Path)
    $bw = New-Object System.IO.BinaryWriter($fs)

    $bw.Write([byte[]]([char[]]"RIFF"))
    $bw.Write([int](36 + $dataSize))
    $bw.Write([byte[]]([char[]]"WAVE"))
    $bw.Write([byte[]]([char[]]"fmt "))
    $bw.Write([int]16)
    $bw.Write([System.Int16]1)
    $bw.Write([System.Int16]1)
    $bw.Write([int]$SampleRate)
    $bw.Write([int]($SampleRate * 2))
    $bw.Write([System.Int16]2)
    $bw.Write([System.Int16]16)
    $bw.Write([byte[]]([char[]]"data"))
    $bw.Write([int]$dataSize)

    foreach ($s in $Samples) {
        $clamped = [Math]::Max(-1.0, [Math]::Min(1.0, $s))
        $bw.Write([System.Int16]($clamped * 32767))
    }

    $bw.Close()
    $fs.Close()
}

function Get-Envelope {
    param([int]$Index, [int]$Total, [double]$Attack = 0.02, [double]$Release = 0.15)

    $t = $Index / $SampleRate
    $dur = $Total / $SampleRate
    $attackSamples = [int]($Attack * $SampleRate)
    $releaseSamples = [int]($Release * $SampleRate)

    $attackEnv = if ($Index -lt $attackSamples) { $Index / [double]$attackSamples } else { 1.0 }
    $releaseStart = $Total - $releaseSamples
    $releaseEnv = if ($Index -gt $releaseStart) { ($Total - $Index) / [double]$releaseSamples } else { 1.0 }
    return $attackEnv * $releaseEnv
}

function New-Tone {
    param(
        [double]$Frequency,
        [double]$Duration,
        [double]$Volume = 0.35,
        [double]$Harmonic = 0.25,
        [double]$Attack = 0.008,
        [double]$Release = 0.12
    )

    $total = [int]($Duration * $SampleRate)
    $samples = New-Object float[] $total
    for ($i = 0; $i -lt $total; $i++) {
        $t = $i / $SampleRate
        $env = Get-Envelope -Index $i -Total $total -Attack $Attack -Release $Release
        $fund = [Math]::Sin(2 * [Math]::PI * $Frequency * $t)
        $harm = [Math]::Sin(2 * [Math]::PI * $Frequency * 2 * $t) * $Harmonic
        $samples[$i] = ($fund + $harm) * $env * $Volume
    }
    return ,$samples
}

function New-Chime {
    param([double[]]$Frequencies, [double]$NoteDuration = 0.09, [double]$Volume = 0.32)

    $all = [System.Collections.Generic.List[float]]::new()
    foreach ($freq in $Frequencies) {
        $note = New-Tone -Frequency $freq -Duration $NoteDuration -Volume $Volume -Release 0.18
        foreach ($s in $note) { [void]$all.Add($s) }
        $gap = [int](0.012 * $SampleRate)
        for ($g = 0; $g -lt $gap; $g++) { [void]$all.Add(0.0) }
    }
    return $all.ToArray()
}

function New-NoiseBurst {
    param([double]$Duration = 0.06, [double]$Volume = 0.18, [double]$Frequency = 180.0)

    $total = [int]($Duration * $SampleRate)
    $rng = [System.Random]::new(42)
    $samples = New-Object float[] $total
    for ($i = 0; $i -lt $total; $i++) {
        $t = $i / $SampleRate
        $env = Get-Envelope -Index $i -Total $total -Attack 0.001 -Release 0.04
        $noise = ($rng.NextDouble() * 2 - 1) * 0.4
        $tone = [Math]::Sin(2 * [Math]::PI * $Frequency * $t) * 0.6
        $samples[$i] = ($noise + $tone) * $env * $Volume
    }
    return ,$samples
}

function New-Whoosh {
    param([double]$Duration = 0.14, [double]$StartFreq = 900, [double]$EndFreq = 200)

    $total = [int]($Duration * $SampleRate)
    $samples = New-Object float[] $total
    for ($i = 0; $i -lt $total; $i++) {
        $progress = $i / [double]$total
        $freq = $StartFreq + ($EndFreq - $StartFreq) * $progress
        $t = $i / $SampleRate
        $env = Get-Envelope -Index $i -Total $total -Attack 0.005 -Release 0.08
        $samples[$i] = [Math]::Sin(2 * [Math]::PI * $freq * $t) * $env * 0.28
    }
    return ,$samples
}

function New-ScanBeep {
    param([double]$Frequency = 1240, [double]$Duration = 0.05)

    $total = [int]($Duration * $SampleRate)
    $samples = New-Object float[] $total
    for ($i = 0; $i -lt $total; $i++) {
        $t = $i / $SampleRate
        $env = Get-Envelope -Index $i -Total $total -Attack 0.002 -Release 0.03
        $square = if ([Math]::Sin(2 * [Math]::PI * $Frequency * $t) -gt 0) { 1.0 } else { -0.3 }
        $samples[$i] = $square * $env * 0.22
    }
    return ,$samples
}

function Combine-Samples {
    param([float[][]]$Parts)
    $total = ($Parts | ForEach-Object { $_.Length } | Measure-Object -Sum).Sum
    $result = New-Object float[] $total
    $offset = 0
    foreach ($part in $Parts) {
        [Array]::Copy($part, 0, $result, $offset, $part.Length)
        $offset += $part.Length
    }
    return ,$result
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$sounds = @{
    "success.wav"  = { New-Chime -Frequencies @(523.25, 659.25, 783.99) -NoteDuration 0.085 -Volume 0.30 }
    "save.wav"     = { Combine-Samples @((New-Tone -Frequency 880 -Duration 0.04 -Volume 0.18 -Release 0.06), (New-Chime -Frequencies @(659.25, 783.99) -NoteDuration 0.07 -Volume 0.26)) }
    "delete.wav"   = { Combine-Samples @((New-Whoosh -Duration 0.12), (New-Tone -Frequency 220 -Duration 0.08 -Volume 0.22 -Release 0.05)) }
    "error.wav"    = { Combine-Samples @((New-Tone -Frequency 180 -Duration 0.12 -Volume 0.30 -Harmonic 0.1 -Release 0.08), (New-NoiseBurst -Duration 0.05 -Volume 0.12 -Frequency 140)) }
    "warning.wav"  = { Combine-Samples @((New-Tone -Frequency 440 -Duration 0.07 -Volume 0.28), (New-Tone -Frequency 554 -Duration 0.09 -Volume 0.28)) }
    "verify.wav"   = { New-Chime -Frequencies @(880, 1174.66) -NoteDuration 0.06 -Volume 0.28 }
    "confirm.wav"  = { New-Tone -Frequency 698.46 -Duration 0.07 -Volume 0.26 -Harmonic 0.15 }
    "cancel.wav"   = { New-Tone -Frequency 349.23 -Duration 0.08 -Volume 0.22 -Harmonic 0.1 -Release 0.10 }
    "info.wav"     = { New-Chime -Frequencies @(587.33, 739.99) -NoteDuration 0.07 -Volume 0.24 }
    "click.wav"    = { New-Tone -Frequency 1200 -Duration 0.025 -Volume 0.15 -Release 0.02 }
    "scan.wav"     = { New-ScanBeep }
    "login.wav"    = { New-Chime -Frequencies @(392, 523.25, 659.25, 783.99) -NoteDuration 0.065 -Volume 0.27 }
    "notification.wav" = { New-Chime -Frequencies @(659.25, 880) -NoteDuration 0.075 -Volume 0.25 }
}

foreach ($entry in $sounds.GetEnumerator()) {
    $path = Join-Path $OutputDir $entry.Key
    $samples = & $entry.Value
    Write-WavFile -Path $path -Samples $samples
    Write-Host "Generated $($entry.Key) ($($samples.Length) samples)"
}

Write-Host "Done. Output: $OutputDir"
