using System.Collections.Generic;
using System.Linq;

namespace VRC_OSC_Handy.NAudio
{
    // Whisper's supported language codes, and the reverse lookup (full name -> code).
    // This is pure static data with nothing to do with capturing audio, so it lives
    // here instead of inside MicrophoneCapture - which still exposes LANGUAGES and
    // TO_LANGUAGE_CODE (forwarding to this class) so every existing call site keeps
    // working unchanged.
    public static class WhisperLanguages
    {
        public static readonly Dictionary<string, string> LANGUAGES = new Dictionary<string, string>
        {
            { "auto", "auto" },
            { "en", "english" },
            { "zh", "chinese" },
            { "de", "german" },
            { "es", "spanish" },
            { "ru", "russian" },
            { "ko", "korean" },
            { "fr", "french" },
            { "ja", "japanese" },
            { "pt", "portuguese" },
            { "tr", "turkish" },
            { "pl", "polish" },
            { "ca", "catalan" },
            { "nl", "dutch" },
            { "ar", "arabic" },
            { "sv", "swedish" },
            { "it", "italian" },
            { "id", "indonesian" },
            { "hi", "hindi" },
            { "fi", "finnish" },
            { "vi", "vietnamese" },
            { "he", "hebrew" },
            { "uk", "ukrainian" },
            { "el", "greek" },
            { "ms", "malay" },
            { "cs", "czech" },
            { "ro", "romanian" },
            { "da", "danish" },
            { "hu", "hungarian" },
            { "ta", "tamil" },
            { "no", "norwegian" },
            { "th", "thai" },
            { "ur", "urdu" },
            { "hr", "croatian" },
            { "bg", "bulgarian" },
            { "lt", "lithuanian" },
            { "la", "latin" },
            { "mi", "maori" },
            { "ml", "malayalam" },
            { "cy", "welsh" },
            { "sk", "slovak" },
            { "te", "telugu" },
            { "fa", "persian" },
            { "lv", "latvian" },
            { "bn", "bengali" },
            { "sr", "serbian" },
            { "az", "azerbaijani" },
            { "sl", "slovenian" },
            { "kn", "kannada" },
            { "et", "estonian" },
            { "mk", "macedonian" },
            { "br", "breton" },
            { "eu", "basque" },
            { "is", "icelandic" },
            { "hy", "armenian" },
            { "ne", "nepali" },
            { "mn", "mongolian" },
            { "bs", "bosnian" },
            { "kk", "kazakh" },
            { "sq", "albanian" },
            { "sw", "swahili" },
            { "gl", "galician" },
            { "mr", "marathi" },
            { "pa", "punjabi" },
            { "si", "sinhala" },
            { "km", "khmer" },
            { "sn", "shona" },
            { "yo", "yoruba" },
            { "so", "somali" },
            { "af", "afrikaans" },
            { "oc", "occitan" },
            { "ka", "georgian" },
            { "be", "belarusian" },
            { "tg", "tajik" },
            { "sd", "sindhi" },
            { "gu", "gujarati" },
            { "am", "amharic" },
            { "yi", "yiddish" },
            { "lo", "lao" },
            { "uz", "uzbek" },
            { "fo", "faroese" },
            { "ht", "haitian creole" },
            { "ps", "pashto" },
            { "tk", "turkmen" },
            { "nn", "nynorsk" },
            { "mt", "maltese" },
            { "sa", "sanskrit" },
            { "lb", "luxembourgish" },
            { "my", "myanmar" },
            { "bo", "tibetan" },
            { "tl", "tagalog" },
            { "mg", "malagasy" },
            { "as", "assamese" },
            { "tt", "tatar" },
            { "haw", "hawaiian" },
            { "ln", "lingala" },
            { "ha", "hausa" },
            { "ba", "bashkir" },
            { "jw", "javanese" },
            { "su", "sundanese" },
            { "yue", "cantonese" }
        };

        // The full-name -> code lookup used to be a second, hand-written ~100-entry
        // dictionary that had to be kept in sync with LANGUAGES above by hand (and
        // occasionally drifted). It's just the reverse of LANGUAGES, plus a handful of
        // alternate spellings Whisper sometimes reports that aren't LANGUAGES' own
        // canonical name for that language.
        public static readonly Dictionary<string, string> TO_LANGUAGE_CODE = BuildToLanguageCode();

        private static Dictionary<string, string> BuildToLanguageCode()
        {
            // "auto" isn't a real language name, so it has no reverse entry.
            Dictionary<string, string> result = LANGUAGES
                .Where(entry => entry.Key != "auto")
                .ToDictionary(entry => entry.Value, entry => entry.Key);

            Dictionary<string, string> synonyms = new Dictionary<string, string>
            {
                { "burmese", "my" },
                { "valencian", "ca" },
                { "flemish", "nl" },
                { "haitian", "ht" },
                { "letzeburgesch", "lb" },
                { "pushto", "ps" },
                { "panjabi", "pa" },
                { "moldavian", "ro" },
                { "moldovan", "ro" },
                { "sinhalese", "si" },
                { "castilian", "es" },
                { "mandarin", "zh" },
            };
            foreach (KeyValuePair<string, string> synonym in synonyms)
                result[synonym.Key] = synonym.Value;

            return result;
        }
    }
}
