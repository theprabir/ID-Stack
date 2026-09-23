# Generates Strings.<culture>.resx files for all supported languages.
# Phase 1 ships translated navigation/menu keys; the remaining keys fall back
# to English via the resource manager satellite mechanism.
$ErrorActionPreference = "Stop"

$destDir = Join-Path $PSScriptRoot "..\..\src\IDStack\Localization"
$resxTemplatePath = Join-Path $destDir "Strings.resx"
$template = Get-Content $resxTemplatePath -Raw -Encoding UTF8

# Keep only the schema/headers part of the template (everything before the first data entry).
$cutoff = $template.IndexOf('  <data name=')
if ($cutoff -lt 0) { throw "Could not locate data section in Strings.resx" }
$header = $template.Substring(0, $cutoff)
$footer = "</root>`n"

$t = @{}
$t["hi-IN"] = @{
  "App.Title" = "ID Stack"; "Menu.File" = "फ़ाइल"; "Menu.Edit" = "संपादन"; "Menu.View" = "दृश्य"; "Menu.Help" = "सहायता";
  "Nav.Home" = "होम"; "Nav.TemplateEditor" = "टेम्पलेट संपादक"; "Nav.TemplateLibrary" = "टेम्पलेट लाइब्रेरी"; "Nav.DataImport" = "डेटा आयात"; "Nav.BatchProcessing" = "बैच प्रोसेसिंग"; "Nav.Settings" = "सेटिंग्स";
  "Home.Welcome" = "ID Stack में आपका स्वागत है"; "Status.Ready" = "तैयार" }
$t["mr-IN"] = @{
  "App.Title" = "ID Stack"; "Menu.File" = "फाइल"; "Menu.Edit" = "संपादन"; "Menu.View" = "दृश्य"; "Menu.Help" = "मदत";
  "Nav.Home" = "मुख्यपृष्ठ"; "Nav.TemplateEditor" = "टेम्पलेट संपादक"; "Nav.TemplateLibrary" = "टेम्पलेट लायब्ररी"; "Nav.DataImport" = "डेटा आयात"; "Nav.BatchProcessing" = "बॅच प्रोसेसिंग"; "Nav.Settings" = "सेटिंग्ज";
  "Home.Welcome" = "ID Stackमध्ये स्वागत आहे"; "Status.Ready" = "तयार" }
$t["or-IN"] = @{
  "App.Title" = "ID Stack"; "Menu.File" = "ଫାଇଲ"; "Menu.Edit" = "ସମ୍ପାଦନା"; "Menu.View" = "ଦୃଶ୍ୟ"; "Menu.Help" = "ସହାୟତା";
  "Nav.Home" = "ମୂଳପୃଷ୍ଠା"; "Nav.TemplateEditor" = "ଟେମ୍ପଲେଟ ସମ୍ପାଦକ"; "Nav.TemplateLibrary" = "ଟେମ୍ପଲେଟ ଲାଇବ୍ରେରୀ"; "Nav.DataImport" = "ତଥ୍ୟ ଆମଦାନୀ"; "Nav.BatchProcessing" = "ବ୍ୟାଚ ପ୍ରସେସିଂ"; "Nav.Settings" = "ସେଟିଂସ";
  "Home.Welcome" = "ID Stackକୁ ସ୍ୱାଗତ"; "Status.Ready" = "ପ୍ରସ୍ତୁତ" }
$t["bn-IN"] = @{
  "App.Title" = "ID Stack"; "Menu.File" = "ফাইল"; "Menu.Edit" = "সম্পাদনা"; "Menu.View" = "ভিউ"; "Menu.Help" = "সাহায্য";
  "Nav.Home" = "হোম"; "Nav.TemplateEditor" = "টেমপ্লেট সম্পাদক"; "Nav.TemplateLibrary" = "টেমপ্লেট লাইব্রেরি"; "Nav.DataImport" = "ডেটা আমদানি"; "Nav.BatchProcessing" = "ব্যাচ প্রসেসিং"; "Nav.Settings" = "সেটিংস";
  "Home.Welcome" = "ID Stackে স্বাগতম"; "Status.Ready" = "প্রস্তুত" }
$t["ta-IN"] = @{
  "App.Title" = "ID Stack"; "Menu.File" = "கோப்பு"; "Menu.Edit" = "திருத்து"; "Menu.View" = "பார்வை"; "Menu.Help" = "உதவி";
  "Nav.Home" = "முகப்பு"; "Nav.TemplateEditor" = "வார்ப்புரு திருத்தி"; "Nav.TemplateLibrary" = "வார்ப்புரு நூலகம்"; "Nav.DataImport" = "தரவு இறக்குமதி"; "Nav.BatchProcessing" = "தொகுதி செயலாக்கம்"; "Nav.Settings" = "அமைப்புகள்";
  "Home.Welcome" = "ID Stack-க்கு வரவேற்கிறோம்"; "Status.Ready" = "தயார்" }
$t["te-IN"] = @{
  "App.Title" = "ID Stack"; "Menu.File" = "ఫైల్"; "Menu.Edit" = "సవరణ"; "Menu.View" = "వీక్షణ"; "Menu.Help" = "సహాయం";
  "Nav.Home" = "హోమ్"; "Nav.TemplateEditor" = "టెంప్లేట్ ఎడిటర్"; "Nav.TemplateLibrary" = "టెంప్లేట్ లైబ్రరీ"; "Nav.DataImport" = "డేటా దిగుమతి"; "Nav.BatchProcessing" = "బ్యాచ్ ప్రాసెసింగ్"; "Nav.Settings" = "సెట్టింగ్‌లు";
  "Home.Welcome" = "ID Stack‌కు స్వాగతం"; "Status.Ready" = "సిద్ధం" }
$t["kn-IN"] = @{
  "App.Title" = "ID Stack"; "Menu.File" = "ಫೈಲ್"; "Menu.Edit" = "ಸಂಪಾದನೆ"; "Menu.View" = "ವೀಕ್ಷಣೆ"; "Menu.Help" = "ಸಹಾಯ";
  "Nav.Home" = "ಮುಖಪುಟ"; "Nav.TemplateEditor" = "ಟೆಂಪ್ಲೇಟ್ ಸಂಪಾದಕ"; "Nav.TemplateLibrary" = "ಟೆಂಪ್ಲೇಟ್ ಲೈಬ್ರರಿ"; "Nav.DataImport" = "ದತ್ತಾಂಶ ಆಮದು"; "Nav.BatchProcessing" = "ಬ್ಯಾಚ್ ಪ್ರೊಸೆಸಿಂಗ್"; "Nav.Settings" = "ಸೆಟ್ಟಿಂಗ್‌ಗಳು";
  "Home.Welcome" = "ID Stack‌ಗೆ ಸ್ವಾಗತ"; "Status.Ready" = "ಸಿದ್ಧ" }
$t["gu-IN"] = @{
  "App.Title" = "ID Stack"; "Menu.File" = "ફાઇલ"; "Menu.Edit" = "સંપાદન"; "Menu.View" = "દૃશ્ય"; "Menu.Help" = "સહાય";
  "Nav.Home" = "હોમ"; "Nav.TemplateEditor" = "ટેમ્પલેટ સંપાદક"; "Nav.TemplateLibrary" = "ટેમ્પલેટ લાઇબ્રેરી"; "Nav.DataImport" = "ડેટા આયાત"; "Nav.BatchProcessing" = "બેચ પ્રોસેસિંગ"; "Nav.Settings" = "સેટિંગ્સ";
  "Home.Welcome" = "ID Stackમાં આપનું સ્વાગત છે"; "Status.Ready" = "તૈયાર" }
$t["pa-IN"] = @{
  "App.Title" = "ID Stack"; "Menu.File" = "ਫਾਈਲ"; "Menu.Edit" = "ਸੋਧ"; "Menu.View" = "ਵੇਖੋ"; "Menu.Help" = "ਮਦਦ";
  "Nav.Home" = "ਘਰ"; "Nav.TemplateEditor" = "ਟੈਂਪਲੇਟ ਸੰਪਾਦਕ"; "Nav.TemplateLibrary" = "ਟੈਂਪਲੇਟ ਲਾਇਬਰੇਰੀ"; "Nav.DataImport" = "ਡਾਟਾ ਆਯਾਤ"; "Nav.BatchProcessing" = "ਬੈਚ ਪ੍ਰੋਸੈਸਿੰਗ"; "Nav.Settings" = "ਸੈਟਿੰਗਾਂ";
  "Home.Welcome" = "ID Stack ਵਿੱਚ ਜੀ ਆਇਆਂ ਨੂੰ"; "Status.Ready" = "ਤਿਆਰ" }
$t["es-ES"] = @{
  "App.Title" = "ID Stack"; "Menu.File" = "Archivo"; "Menu.Edit" = "Editar"; "Menu.View" = "Ver"; "Menu.Help" = "Ayuda";
  "Nav.Home" = "Inicio"; "Nav.TemplateEditor" = "Editor de Plantillas"; "Nav.TemplateLibrary" = "Biblioteca de Plantillas"; "Nav.DataImport" = "Importación de Datos"; "Nav.BatchProcessing" = "Procesamiento por Lotes"; "Nav.Settings" = "Configuración";
  "Home.Welcome" = "Bienvenido al ID Stack"; "Status.Ready" = "Listo" }

function Escape-Xml([string]$s) {
  return $s.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;')
}

foreach ($culture in $t.Keys) {
  $entries = $t[$culture]
  $dataXml = ""
  foreach ($key in $entries.Keys) {
    $value = Escape-Xml $entries[$key]
    $dataXml += "  <data name=`"$key`" xml:space=`"preserve`">`n    <value>$value</value>`n  </data>`n"
  }
  $content = $header + $dataXml + $footer
  $outPath = Join-Path $destDir "Strings.$culture.resx"
  [System.IO.File]::WriteAllText($outPath, $content, (New-Object System.Text.UTF8Encoding $true))
  Write-Host "Wrote $outPath"
}
