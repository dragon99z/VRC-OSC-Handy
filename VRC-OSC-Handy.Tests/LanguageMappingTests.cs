using System.Linq;
using VRC_OSC_Handy.NAudio;
using Xunit;

namespace VRC_OSC_Handy.Tests
{
    // MicrophoneCapture.LANGUAGES drives the STT language dropdown (by index, see
    // MainWindow's use of LANGUAGES.Keys.ElementAt(...)), and TO_LANGUAGE_CODE maps
    // a display name back to a Whisper language code. These tests just guard the data
    // stays internally consistent - no audio/model loading involved.
    public class LanguageMappingTests
    {
        [Fact]
        public void Languages_HasNoDuplicateCodesOrNames()
        {
            var codes = MicrophoneCapture.LANGUAGES.Keys.ToList();
            var names = MicrophoneCapture.LANGUAGES.Values.ToList();

            Assert.Equal(codes.Count, codes.Distinct().Count());
            Assert.Equal(names.Count, names.Distinct().Count());
        }

        [Fact]
        public void Languages_IncludesAutoDetect()
        {
            Assert.True(MicrophoneCapture.LANGUAGES.ContainsKey("auto"));
        }

        [Theory]
        [InlineData("en")]
        [InlineData("fr")]
        [InlineData("de")]
        [InlineData("ja")]
        public void Languages_ContainsCommonLanguageCodes(string code)
        {
            Assert.True(MicrophoneCapture.LANGUAGES.ContainsKey(code));
        }

        [Fact]
        public void EveryLanguageName_RoundTripsThroughToLanguageCode()
        {
            // For every code/name pair in LANGUAGES (besides the special "auto" entry,
            // which has no Whisper language code of its own), looking the display name
            // back up in TO_LANGUAGE_CODE must return the same code it came from.
            foreach (var pair in MicrophoneCapture.LANGUAGES)
            {
                if (pair.Value == "auto")
                    continue;

                Assert.True(
                    MicrophoneCapture.TO_LANGUAGE_CODE.TryGetValue(pair.Value, out string code),
                    $"'{pair.Value}' (code '{pair.Key}') is missing from TO_LANGUAGE_CODE.");
                Assert.Equal(pair.Key, code);
            }
        }
    }
}
