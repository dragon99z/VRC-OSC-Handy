using VRC_OSC_Handy.Osc;
using Xunit;

namespace VRC_OSC_Handy.Tests
{
    // Covers VRCOSC's pure helper methods only (TranslateValue, ReverseTranslateValue,
    // GenerateProgressBar). Everything else on VRCOSC talks to OSC/Spotify/VoiceMeeter
    // and needs real hardware/services, so it's out of scope for a unit test.
    public class VRCOSCTests
    {
        readonly VRCOSC osc = new VRCOSC();

        [Theory]
        [InlineData(-1f, -60f)]
        [InlineData(0f, -24f)]
        [InlineData(1f, 12f)]
        public void TranslateValue_MapsKnownPoints(float input, float expected)
        {
            Assert.Equal(expected, osc.TranslateValue(input), 3);
        }

        [Theory]
        [InlineData(-5f, -60f)] // below -1 clamps to -1 -> -60
        [InlineData(5f, 12f)]   // above 1 clamps to 1 -> 12
        public void TranslateValue_ClampsOutOfRangeInput(float input, float expected)
        {
            Assert.Equal(expected, osc.TranslateValue(input), 3);
        }

        [Theory]
        [InlineData(-60f, -1f)]
        [InlineData(-24f, 0f)]
        [InlineData(12f, 1f)]
        public void ReverseTranslateValue_MapsKnownPoints(float input, float expected)
        {
            Assert.Equal(expected, osc.ReverseTranslateValue(input), 3);
        }

        [Theory]
        [InlineData(-1f)]
        [InlineData(-0.5f)]
        [InlineData(0f)]
        [InlineData(0.37f)]
        [InlineData(1f)]
        public void TranslateValue_And_ReverseTranslateValue_AreInverses(float input)
        {
            // This is the round trip the VoiceMeeter gain slider relies on: send TranslateValue(x)
            // to VoiceMeeter, read it back and run it through ReverseTranslateValue, and get x back.
            float roundTripped = osc.ReverseTranslateValue(osc.TranslateValue(input));
            Assert.Equal(input, roundTripped, 3);
        }

        [Fact]
        public void GenerateProgressBar_HalfwayIsHalfFilled()
        {
            string bar = osc.GenerateProgressBar(50, 100, 10);
            Assert.Equal("█████▒▒▒▒▒", bar);
        }

        [Fact]
        public void GenerateProgressBar_AtStartIsEmpty()
        {
            string bar = osc.GenerateProgressBar(0, 100, 10);
            Assert.Equal("▒▒▒▒▒▒▒▒▒▒", bar);
        }

        [Fact]
        public void GenerateProgressBar_AtEndIsFull()
        {
            string bar = osc.GenerateProgressBar(100, 100, 10);
            Assert.Equal("██████████", bar);
        }

        [Fact]
        public void GenerateProgressBar_TimestampPastDuration_DoesNotThrow()
        {
            // Regression test: Spotify's reported progress can reach/exceed the track
            // duration for a moment near the end of a song. This used to throw
            // ArgumentOutOfRangeException instead of just showing a full bar.
            string bar = osc.GenerateProgressBar(120, 100, 10);
            Assert.Equal("██████████", bar);
        }

        [Fact]
        public void GenerateProgressBar_ZeroDuration_DoesNotThrow()
        {
            string bar = osc.GenerateProgressBar(0, 0, 10);
            Assert.Equal("▒▒▒▒▒▒▒▒▒▒", bar);
        }
    }
}
