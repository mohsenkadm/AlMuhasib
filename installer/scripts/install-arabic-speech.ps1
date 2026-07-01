# Installs Arabic speech recognition language components for Qayd voice assistant.
param(
    [string[]] $Languages = @('ar-SA', 'ar-IQ', 'ar-EG', 'ar-AE')
)

$ErrorActionPreference = 'Continue'

function Write-InstallStatus {
    param([string] $Message, [int] $Percent, [string] $Step)
    Write-Host "[$Step] $Message"
    Write-Progress -Activity 'تثبيت حزمة اللغة العربية لقيد' -Status $Message -PercentComplete $Percent -CurrentOperation $Step
}

Add-Type -AssemblyName System.Speech

function Test-ArabicRecognizer {
    return [System.Speech.Recognition.SpeechRecognitionEngine]::InstalledRecognizers() |
        Where-Object { $_.Culture.TwoLetterISOLanguageName -eq 'ar' } |
        Select-Object -First 1
}

Write-InstallStatus 'جاري التحقق من حزمة التعرف الصوتي...' 3 'الخطوة 1 من 6'

if (Test-ArabicRecognizer) {
    Write-InstallStatus 'حزمة العربية مثبتة مسبقاً.' 100 'اكتمل'
    exit 0
}

$step = 1
foreach ($language in $Languages) {
    $step++
    $percent = 8 + ($step * 14)
    try {
        Write-InstallStatus "جاري تنزيل وتثبيت حزمة اللغة ($language)..." $percent "الخطوة $step من 6"
        Install-Language -Language $language -ErrorAction Stop
    }
    catch {
        Write-Warning "Install-Language failed for ${language}: $($_.Exception.Message)"
    }

    if (Test-ArabicRecognizer) {
        Write-InstallStatus "تم تثبيت محرك التعرف الصوتي ($language)." 100 'اكتمل'
        exit 0
    }
}

Write-InstallStatus 'جاري تثبيت مكوّنات اللغة الإضافية...' 72 'الخطوة 5 من 6'

$capabilities = @(
    'Language.Basic~~~ar-SA~0.0.1.0',
    'Language.TextToSpeech~~~ar-SA~0.0.1.0',
    'Language.OCR~~~ar-SA~0.0.1.0'
)

foreach ($capability in $capabilities) {
    try {
        Write-InstallStatus "جاري إضافة: $capability" 78 'الخطوة 5 من 6'
        & dism.exe /Online /NoRestart /Add-Capability /CapabilityName:$capability | Out-Null
    }
    catch {
        Write-Warning "DISM failed for ${capability}: $($_.Exception.Message)"
    }
}

Write-InstallStatus 'جاري تفعيل محرك التعرف الصوتي...' 88 'الخطوة 6 من 6'
for ($attempt = 1; $attempt -le 8; $attempt++) {
    if (Test-ArabicRecognizer) {
        Write-InstallStatus 'تم تثبيت حزمة التعرف الصوتي بالعربية بنجاح.' 100 'اكتمل'
        exit 0
    }

    $percent = 88 + ($attempt * 1.5)
    Write-InstallStatus "بانتظار اكتمال التثبيت... ($attempt/8)" $percent 'الخطوة 6 من 6'
    Start-Sleep -Seconds 2
}

Write-InstallStatus 'تعذر إكمال التثبيت التلقائي.' 100 'تعذر التثبيت'
Write-Warning 'Arabic speech recognizer is still unavailable after installation attempts.'
exit 1
